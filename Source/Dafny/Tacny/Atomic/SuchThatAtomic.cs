using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Dafny.Tacny.Expr;


namespace Microsoft.Dafny.Tacny.Atomic {
  class SuchThatAtomic : Atomic{
    public override string Signature => ":|";
    public override int ArgsCount => -1;

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state){
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
        foreach (var item in suchThat.Lhss){
          if (item is IdentifierExpr){
            var id = (IdentifierExpr) item;
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
        var dest_expr = (suchThat.Expr as BinaryExpr);
        var l = (Expression)SimpTacticExpr.SimpTacExpr(state, dest_expr.E1);
        //TODO: currently assume the op is always "in"
        foreach (var item in (l as SetDisplayExpr).Elements) {
          var copy = state.Copy();
          copy.UpdateTacnyVar(local, item);
          yield return copy;
        }
      }
    }
  }
}

