using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.Dafny.Tacny.Atomic {
  /// <summary>
  ///   Abstract class for Atomic Statement
  /// </summary>
  [ContractClass(typeof(AtomicContract))]
  public abstract class Atomic : BaseTactic {
    public abstract override string Signature { get; }

    /// <summary>
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public abstract IEnumerable<ProofState> Generate(Statement statement, ProofState state);

    protected static List<Expression> GetCallArguments(UpdateStmt us) {
      Contract.Requires(us != null);
      var er = (ExprRhs)us.Rhss[0];
      return ((ApplySuffix)er.Expr).Args;
    }

    protected static void InitArgs(ProofState ps, Statement st, out IVariable lv, out List<Expression> callArguments) {
      Contract.Requires(st != null);
      Contract.Ensures(Contract.ValueAtReturn(out callArguments) != null);
      lv = null;
      callArguments = null;
      TacnyBlockStmt tbs;

      // tacny variables should be declared as tvar or tactic var
      //if(st is VarDeclStmt)
      //  Contract.Assert(false, Error.MkErr(st, 13));

      if(st is TacticVarDeclStmt){
        var tvds = st as TacticVarDeclStmt;
        lv = tvds.Locals[0];
        callArguments = GetCallArguments(tvds.Update as UpdateStmt);

      } else if (st is TacticInvariantStmt){
        callArguments = new List<Expression>();
        var expr = (st as TacticInvariantStmt).Expr as ParensExpression;
        callArguments.Add(expr.E);
        
      }else if(st is UpdateStmt) {
        var us = st as UpdateStmt;
        if(us.Lhss.Count == 0)
          callArguments = GetCallArguments(us);
        else {
          var ns = (NameSegment)us.Lhss[0];
          if(ps.ContainTacnyVal(ns)) {
            //TODO: need to doubel check this
            lv = ps.GetTacnyVarValue(ns) as IVariable;
            callArguments = GetCallArguments(us);
          }
        }
      } else if((tbs = st as TacnyBlockStmt) != null) {
        var pe = tbs.Guard as ParensExpression;
        callArguments = pe != null ? new List<Expression> { pe.E } : new List<Expression> { tbs.Guard };
      }

    }
  }

  [ContractClassFor(typeof(Atomic))]
  public class AtomicContract : Atomic {
    public override string Signature { get; }
    public override int ArgsCount { get; }

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state) {
      Contract.Requires(statement != null);
      Contract.Requires(state != null);

      yield break;
    }
  }
}