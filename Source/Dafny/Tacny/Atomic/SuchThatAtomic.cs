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
        //hardcode the operator as "in"
        var destExpr = (suchThat.Expr as BinaryExpr);
        if (destExpr != null)
        {
          object l;
          try
          {
            l = SimpTacticExpr.SimpTacExpr(state, destExpr.E1);
          }
          catch (Exception e)
          {
            l = SimpTacticExpr.EvalTacticExpr(state, destExpr.E1);
          }

          if (l is SetDisplayExpr){
            var setDisplayExpr = l as SetDisplayExpr;
            foreach (var item in setDisplayExpr.Elements)
            {
              var copy = state.Copy();
              copy.UpdateTacnyVar(local, item);
              copy.TopTokenTracer().AddBranchTrace(count);
              yield return copy;
              count++;
            }
          } else if (l is List<MemberDecl>)
          {
            var objList = l as List<MemberDecl>;
            foreach (var item in objList) {
              var copy = state.Copy();
              copy.UpdateTacnyVar(local, item);
              copy.TopTokenTracer().AddBranchTrace(count);
              yield return copy;
              count++;
            }
          } else if (l is List<IVariable>)
          {
            var objList = l as List<IVariable>;
            foreach (var item in objList)
            {
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
}

