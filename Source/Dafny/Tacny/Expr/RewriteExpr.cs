using Microsoft.Boogie;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Basetypes;

namespace Microsoft.Dafny.Tacny.Expr {
  class EvalExpr : SimpExpr{
    protected EvalExpr(ProofState state) : base(state){}

    internal bool EqLiteralExprs(LiteralExpr e0, LiteralExpr e1){
      if (e0.Value is bool && e1.Value is bool){
        return ((bool)e0.Value) == ((bool)e1.Value);
      } else if (e0.Value is int && e1.Value is int){
        return ((int) e0.Value) == ((int) e1.Value);
      } else if (e0.Value is BigInteger && e1.Value is BigInteger){
         return BigInteger.Compare((BigInteger)e0.Value, (BigInteger)e1.Value) == 0;
      } else if (e0.Value is string && e1.Value is string){
        return String.Compare((string) e0.Value, (string) e1.Value) == 0;
      }
      
      return false;
    }

    internal bool EqExpr(Expression e0, Expression e1){
      if (e0 is LiteralExpr && e1 is LiteralExpr){
        return EqLiteralExprs((LiteralExpr) e0, (LiteralExpr) e1);
      }
      
      return false;
    }

    internal List<Expression> NormaliseSet(List<Expression> l){
      var newL = new List<Expression>();
      foreach (var item in l){
        if(newL.Find(y => EqExpr(item, y)) == null)
          newL.Add(item);
      }
      return newL;
    }
    public override Expression CloneExpr(Expression expr){
      if (expr is UnaryExpr){
      } else if (expr is BinaryExpr){
        var binExpr = expr as BinaryExpr;
        var e0 = CloneExpr(binExpr.E0);
        var e1 = CloneExpr(binExpr.E1);

        switch(binExpr.Op) {
          case BinaryExpr.Opcode.Add:
            if (e0 is LiteralExpr && e1 is LiteralExpr){
              var value =  BigInteger.Add(
                (BigInteger) (e0 as LiteralExpr).Value,
                (BigInteger) (e1 as LiteralExpr).Value
                );
              return new LiteralExpr(new Token(Interpreter.TacticCodeTokLine,0), value);
            } else if (e0 is SetDisplayExpr && e1 is SetDisplayExpr){

              var newEles = new List<Expression>();
              newEles.AddRange((e0 as SetDisplayExpr).Elements);
              newEles.AddRange((e1 as SetDisplayExpr).Elements);
               
              return new SetDisplayExpr(new Token(Interpreter.TacticCodeTokLine,0), 
                (e0 as SetDisplayExpr).Finite, NormaliseSet(newEles));
            } else {
              return new BinaryExpr(new Token(Interpreter.TacticCodeTokLine, 0),
                binExpr.Op, e0, e1);
            }

            break;
          default:
            break;
        }

      } else if (expr is TernaryExpr) { }

      return base.CloneExpr(expr);
    }

    public static Expression EvalTacticExpression(ProofState state, Expression expr){
      var rewriter = new EvalExpr(state);
      return rewriter.CloneExpr(expr);
    }
  }

  /// <summary>
  /// only simplify tactic expression
  /// </summary>
  class SimpExpr : Cloner{
    private readonly ProofState _state;

    protected SimpExpr(ProofState state) {
      _state = state;
    }

    public abstract class EvalValRet { }

    public class BooleanRet : EvalValRet
    {
      public bool Value;
    }
    public class IntRet : EvalValRet
    {
      public int Value;
    }

    public class ExprRet : EvalValRet
    {
      public Expression Value;
    }

    internal bool IsTVar(Expression expr)
    {
      var segment = expr as NameSegment;
      return (segment != null && _state.ContainTVal(segment.Name));
    }

    internal object EvalTVar(NameSegment ns, bool deref){
      var value = _state.GetTVarValue(ns.Name);
      if (deref && value is Expression && IsTVar(value as Expression))
        return EvalTVar(value as NameSegment, true);
      else
        return value;
    }

    internal bool IsEAtmoicCall(ApplySuffix aps){
      return EAtomic.EAtomic.IsEAtomicSig(Util.GetSignature(aps));
    }

    internal object EvalEAtomExpr(ApplySuffix aps){
      Contract.Requires(EAtomic.EAtomic.IsEAtomicSig(Util.GetSignature(aps)));
      var sig = Util.GetSignature(aps);
      var types = Assembly.GetAssembly(typeof(EAtomic.EAtomic)).GetTypes()
        .Where(t => t.IsSubclassOf(typeof(EAtomic.EAtomic)));
      foreach (var eType in types){
        var eatomInst = Activator.CreateInstance(eType) as EAtomic.EAtomic;
        if (sig == eatomInst?.Signature){
          //TODO: validate input countx
            return eatomInst?.Generate(aps, _state);
        }
      }
      return null;
    }

    internal bool IsETacticCall(ApplySuffix aps){
      //TODO: this is for expression tactic call, e.g. funtion tactic
      return false;
    }

    internal object EvalETacticCall(ApplySuffix aps){
      //fucntion expression call, yet to be supported
      throw new NotImplementedException();
    }

    public static object UnfoldTacticProjection(ProofState state, Expression expr){
      var e = new SimpExpr(state);
      if (e.IsTVar(expr))
        return e.EvalTVar(expr as NameSegment, true);
      else
      {
        var suffix = expr as ApplySuffix;
        if (suffix != null){
          var aps = suffix;
          if (e.IsEAtmoicCall(aps))
            return e.EvalEAtomExpr(aps);
          else if (e.IsETacticCall(aps))
            return e.IsETacticCall(aps);
        }
      }
      //TODO: evaluate expr as boolean and return as BooleanRet
      return null;
    }

    public static Statement SimpTacticExpr(ProofState state, Statement stmt){
      var simplifier = new SimpExpr(state);
      return simplifier.CloneStmt(stmt);
    }

    public static Expression SimpTacticExpr(ProofState state, Expression expr){
      var simplifier = new SimpExpr(state);
      return simplifier.CloneExpr(expr);
    }

    public override Expression CloneApplySuffix(ApplySuffix e){
      if (IsEAtmoicCall(e)){
         var obj = EvalEAtomExpr(e);
        if (obj is Expression)
          return obj as Expression;
        else if (obj is List<Expression>)
          return new SetDisplayExpr(new Token(Interpreter.TacticCodeTokLine,0), true, obj as List<Expression>);
        else
          throw new NotSupportedException ("Unknown type to handle when simplifying an expression");

      }
      else if (IsETacticCall(e)){
        return (EvalETacticCall(e) as Expression);
      }
      else
        return base.CloneApplySuffix(e);
    }

    public override Expression CloneNameSegment(Expression expr){
      if (IsTVar(expr))
        return (EvalTVar(expr as NameSegment, true) as Expression);
      return base.CloneNameSegment(expr);
    }

    public override Expression CloneExpr(Expression expr)
    {
      var suffix = expr as ApplySuffix;
      if (suffix != null)
        return CloneApplySuffix(suffix);
      else if (expr is NameSegment){
        return (CloneNameSegment(expr));
      }
      else
        return base.CloneExpr(expr);
    }

  }

  class RenameVar : Cloner {

    private readonly Dictionary<string, string> _renames;

    public RenameVar() {
      _renames = new Dictionary<string, string>();
    }

    public void AddRename(String before, String after) {
      _renames.Add(before,after);
    }

    
    public override BoundVar CloneBoundVar(BoundVar bv) {
      String nm;
      var name = _renames.TryGetValue(bv.Name, out nm) ? nm : bv.Name;
      var bvNew = new BoundVar(Tok(bv.tok), name, CloneType(bv.Type)) {IsGhost = bv.IsGhost};
      return bvNew;
    }
 
    public override Expression CloneNameSegment(Expression expr) {
      var e = (NameSegment) expr;
      String nm;
      var name = _renames.TryGetValue(e.Name, out nm) ? nm : e.Name;
      return new NameSegment(Tok(e.tok), name,
        e.OptTypeArguments?.ConvertAll(CloneType));
    }
  }

}