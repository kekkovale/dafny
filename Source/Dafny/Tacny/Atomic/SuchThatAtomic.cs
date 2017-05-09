using System;
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
          if (item is IdentifierExpr){
            var id = item as IdentifierExpr;
            if (state.ContainTVal(id.Name))
              locals.Add(id.Name);
          } else if (item is NameSegment){
            var id = item as NameSegment;
            if(!state.ContainTVal(id.Name))
              yield break;
            else{
              locals.Add(id.Name);
            }
          }
        }
      }
      else{
        locals = new List<string>(tvds.Locals.Select(x => x.Name).ToList());
      }

      foreach (var local in locals){
        //hardcode the operator as "in"
        var destExpr = (suchThat.Expr as BinaryExpr);
        if (destExpr != null)
        {
          Expression  l = SimpExpr.SimpTacticExpr(state, destExpr.E1);
          
          if (l is SeqDisplayExpr){
            var seqDisplayExpr = l as SeqDisplayExpr;
            foreach (var item in seqDisplayExpr.Elements)
            {
              var copy = state.Copy();
              copy.UpdateTacticVar(local, item);
              yield return copy;
              count++;
            }
          } 
        }
      }
    }
  }
}

