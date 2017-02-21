using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.Expr {
  public class BooleanRet {
    public bool value;
  }

  class SimpTaticExpr : Cloner{
    private ProofState _state;

    private SimpTaticExpr(ProofState state) {
      this._state = state;
    }

    internal bool IsTVar(Expression expr){
      return (expr is NameSegment && _state.ContainTVal((expr as NameSegment).Name));
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

    internal object EvalETacCall(ApplySuffix aps){
      //fucntion expression call, yet to be supported
      throw new NotImplementedException();
    }

    public static object EvalTacExpr(ProofState state, Expression expr){
      var e = new SimpTaticExpr(state);
      if (e.IsTVar(expr))
        return e.EvalTVar(expr as NameSegment, true);
      else if (expr is ApplySuffix){
        var aps = expr as ApplySuffix;
        if (e.IsEAtmoicCall(aps))
          return e.EvalEAtomExpr(aps);
        else if (e.IsETacticCall(aps))
          return e.IsETacticCall(aps);
      }
      //TODO: evaluate expr as boolean and return as BooleanRet
      return null;
    }

    public static Statement SimpTacExpr(ProofState state, Statement stmt){
      var cloner = new SimpTaticExpr(state);
      return cloner.CloneStmt(stmt);
    }

    public static Expression SimpTacExpr(ProofState state, Expression expr){
      var cloner = new SimpTaticExpr(state);
      return cloner.CloneExpr(expr);
    }

    public override Expression CloneApplySuffix(ApplySuffix e){
      if (IsEAtmoicCall(e)){
        return (EvalEAtomExpr(e) as Expression);
      }
      else if (IsETacticCall(e)){
        return (EvalETacCall(e) as Expression);
      }
      else
        return base.CloneApplySuffix(e);
    }

    public override Expression CloneNameSegment(Expression expr){
      if (IsTVar(expr))
        return (EvalTVar(expr as NameSegment, true) as Expression);
      return base.CloneNameSegment(expr);
    }

    public override Expression CloneExpr(Expression expr){
      if (expr is ApplySuffix)
        return CloneApplySuffix(expr as ApplySuffix);
      else if (expr is NameSegment){
        return (CloneNameSegment(expr));
      }
      else
        return base.CloneExpr(expr);
    }

  }

  class RenameVar : Cloner {

    private Dictionary<String, String> _renames;

    public RenameVar() : base() {
      _renames = new Dictionary<String, String>();
    }

    public void AddRename(String before, String after) {
      _renames.Add(before,after);
    }

    /*
    public override BoundVar CloneBoundVar(BoundVar bv) { 
      var bvNew = new BoundVar(Tok(bv.tok), "dummy", CloneType(bv.Type));
      bvNew.IsGhost = bv.IsGhost;
      return bvNew;
    }
    */

    public override Expression CloneNameSegment(Expression expr) {
      var e = (NameSegment) expr;
      String nm, name;
      if (_renames.TryGetValue(e.Name, out nm))
        name = nm;
      else {
        name = e.Name;
      }
      return new NameSegment(Tok(e.tok), name,
        e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
    }
  }

}