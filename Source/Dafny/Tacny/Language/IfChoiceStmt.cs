using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny.Tacny.Expr;
using System.Diagnostics.Contracts;

namespace Microsoft.Dafny.Tacny.Language {
  public class IfChoiceStmt : TacticFrameCtrl{

    public override bool MatchStmt(Statement stmt, ProofState state){
      if (stmt is Dafny.IfStmt || stmt is Dafny.AlternativeStmt)
        return (new OrChoiceStmt().MatchStmt(stmt, state) == false);
      return false;
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var statement = GetStmt();
      return Interpreter.EvalStmt(statement, state0);
    }

    private List<Tuple<Expression, List<Statement>>> GetGuadBodyList(Statement stmt, List<Tuple<Expression, List<Statement>>> resList){
      if (stmt is IfStmt) { 
        return GetGuadBodyList(stmt as IfStmt, resList);
      }
      else{
        return GetGuadBodyList(stmt as AlternativeStmt, resList);
      }
    }

    private List<Tuple<Expression, List<Statement>>> GetGuadBodyList(IfStmt stmt, List<Tuple<Expression, List<Statement>>> resList){
      // the else stmt will have expression true as the guard
      resList.Add(new Tuple<Expression, List<Statement>>(stmt.Guard, stmt.Thn.Body));
      if (stmt.Els == null)
        return resList;
      else if (stmt.Els is BlockStmt){ // else
        var body = stmt.Els as BlockStmt;
        resList.Add(new Tuple<Expression, List<Statement>>(null, body.Body));
        return resList;
      } else { // else if
        return GetGuadBodyList(stmt.Els, resList);
      }
    }
    private List<Tuple<Expression, List<Statement>>> GetGuadBodyList(AlternativeStmt stmt, List<Tuple<Expression, List<Statement>>> resList){
      //for each cases, add guards and body into the list
      foreach (var item in stmt.Alternatives){
        resList.Add(new Tuple<Expression, List<Statement>>(item.Guard, item.Body));
      }
      return resList;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      bool partial = true || state0.IsCurFramePartial();
      List<Tuple<Expression, List<Statement>>>  guardBodyList = new List<Tuple<Expression, List<Statement>>>();

      int counter = 0;

      guardBodyList = GetGuadBodyList(statement, guardBodyList);
      Contract.Assert(guardBodyList.Count > 0);


      //check whether the top level of the first guard is tactic level or object level
      if (!EvalExpr.IsTopLevelTacExpr(state0, guardBodyList[0].Item1))
      {
        foreach (var st in EvalExpr.SimpTacExpr(state0, statement)) {
          var state = state0.Copy();
          state.NeedVerify = true;
          state.AddStatement(st);
          yield return state;
        }
        yield break;
      } else { // tactic if
        foreach (var item in guardBodyList) {
          if (item.Item1 == null && counter == 0) //else branch and no other branch is satisfied.
          {
            counter++;
            var state = state0.Copy();
            var ifChoice = this.Copy();
            ifChoice.IsPartial = partial;
            ifChoice.InitBasicFrameCtrl(item.Item2, null);
            state.AddNewFrame(ifChoice);

            yield return state;
          } else {
            var guardExpressionTree = ExpressionTree.ExpressionToTree(item.Item1);
            foreach (var res in ExpressionTree.ResolveExpression(guardExpressionTree, state0)) {
              if (ExpressionTree.EvaluateEqualityExpression(res, state0)) {
                counter++;
                var state = state0.Copy();
                var ifChoice = this.Copy();
                ifChoice.InitBasicFrameCtrl(item.Item2, null);
                ifChoice.IsPartial = partial;
                state.AddNewFrame(ifChoice);
                yield return state;
              }
            }
          }
        }

        //no condition can be found, then do nothing
        if (counter == 0)
          yield return state0.Copy();
      } 
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps){
      return childFrameRes;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}