using Microsoft.Boogie;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Microsoft.Dafny.Tacny.Expr {
  public class BooleanRet {
    public bool Value;
  }

  class SimpTacticExpr : Cloner{
    private readonly ProofState _state;

    private SimpTacticExpr(ProofState state) {
      _state = state;
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

    public static object EvalTacticExpr(ProofState state, Expression expr){
      var e = new SimpTacticExpr(state);
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

    public static Statement SimpTacExpr(ProofState state, Statement stmt){
      var cloner = new SimpTacticExpr(state);
      return cloner.CloneStmt(stmt);
    }

    public static Expression SimpTacExpr(ProofState state, Expression expr){
      var cloner = new SimpTacticExpr(state);
      return cloner.CloneExpr(expr);
    }

    public override Expression CloneApplySuffix(ApplySuffix e){
      if (IsEAtmoicCall(e)){
         var obj = EvalEAtomExpr(e);
        if (obj is Expression)
          return obj as Expression;
        else if (obj is List<Expression>)
          return new SetDisplayExpr(new Token(Interpreter.TacnyCodeTokLine,0), true, obj as List<Expression>);
        else
          throw new NotSupportedException ("Unkonwn type to handle when simplifying an expression");

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