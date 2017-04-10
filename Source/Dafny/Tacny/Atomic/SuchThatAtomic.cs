using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Dafny.Tacny.Expr;


namespace Microsoft.Dafny.Tacny.Atomic {
  class SuchThatAtomic : Atomic{
    public override string Signature => ":|";
    public override int ArgsCount => -1;

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state){
      var count = 0;
      var tvds = statement as TacticVarDeclStmt;
      AssignSuchThatStmt suchThat = null;
      if (tvds != null)
        suchThat = tvds.Update as AssignSuchThatStmt;
      else if (statement is AssignSuchThatStmt){
        suchThat = (AssignSuchThatStmt) statement;
      }
      else{
        Contract.Assert(false, "Unexpected statement type");
      }
      Contract.Assert(suchThat != null, "Unexpected statement type");

      var locals = new List<string>();
      if (tvds == null){
        foreach (var item in suchThat.Lhss)
        {
          var expr = item as IdentifierExpr;
          if (expr != null){
            var id = expr;
            if (state.ContainTVal(id.Name))
              locals.Add(id.Name);
            else{
              //TODO: error
            }
          }
        }
      }
      else{
        locals = new List<string>(tvds.Locals.Select(x => x.Name).ToList());
      }

      foreach (var local in locals){
        var destExpr = (suchThat.Expr as BinaryExpr);
        if (destExpr != null)
        {
          var l = (Expression)SimpTacticExpr.SimpTacExpr(state, destExpr.E1);
          //TODO: currently assume the op is always "in"
          var setDisplayExpr = l as SetDisplayExpr;
          if (setDisplayExpr != null)
            foreach (var item in setDisplayExpr.Elements) {
              var copy = state.Copy();
              copy.UpdateTacnyVar(local, item);
              copy.TopTokenTracer().AddBranchTrace(count);
              yield return copy;
              count++;
            }
        }
      }
    }
  }
}

