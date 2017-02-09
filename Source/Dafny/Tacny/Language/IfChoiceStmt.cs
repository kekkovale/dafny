using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny.Tacny.Expr;

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
      List < Tuple < Expression, List < Statement >>>  guardBodyList = new List<Tuple<Expression, List<Statement>>>();

      int counter = 0;

      guardBodyList = GetGuadBodyList(statement, guardBodyList);
      foreach (var item in guardBodyList){
        //TODO: to implement more complete expression evaluation
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
          foreach (var res in ExpressionTree.ResolveExpression(guardExpressionTree, state0)){
            if (ExpressionTree.EvaluateEqualityExpression(res, state0)){
              counter ++;
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
      if(counter == 0)
        yield return state0.Copy();
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps){
      //terminate as long as one branch is successful
      return _rawCodeList.Count == 1;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
/*
namespace  Microsoft.Dafny.Tacny.Language {
  public class IfStmt : FlowControlStmt {
    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state) {

      var guard = ExtractGuard(statement);

      Contract.Assert(guard != null, "guard");
      return IsResolvable(guard, state) ? EvaluateIf(statement as dfy.IfStmt, guard, state) :
        InsertIf(statement as dfy.IfStmt, guard, state);
    }

    /// <summary>
    /// Evaluate tacny level if statement
    /// </summary>
    /// <param name="ifStmt"></param>
    /// <param name="guard"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    private IEnumerable<ProofState> EvaluateIf(dfy.IfStmt ifStmt, ExpressionTree guard, ProofState state) {
      if (ExpressionTree.EvaluateEqualityExpression(guard, state)) {
        return Interpreter.EvaluateBlockStmt(state, ifStmt.Thn);
      }
      if (ifStmt.Els == null) return null;
      var els = ifStmt.Els as BlockStmt;
      return els != null ? Interpreter.EvaluateBlockStmt(state, els) :
        EvaluateIf(ifStmt.Els as dfy.IfStmt, ExtractGuard(ifStmt.Els), state);
    }

    /// <summary>
    /// Insert the if statement into dafny code
    /// </summary>
    /// <param name="ifStmt"></param>
    /// <param name="guard"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    private IEnumerable<ProofState> InsertIf(dfy.IfStmt ifStmt, ExpressionTree guard, ProofState state) {
      // resolve the if statement guard
      foreach (var newGuard in ExpressionTree.ResolveExpression(guard, state)) {
        var expr = newGuard.TreeToExpression();
        var p = new Printer(Console.Out);
        p.PrintExpression(expr, false);
        newGuard.SetParent();
        var resultExpression = newGuard.TreeToExpression();
        // resolve 'if' block
        var ifStmtEnum = Interpreter.EvaluateBlockStmt(state, ifStmt.Thn);
        IEnumerable<ProofState> elseStmtEnum = null;
        if (ifStmt.Els != null) {
          // resovle else block
          var stmt = ifStmt.Els as BlockStmt;
          elseStmtEnum = stmt != null ? Interpreter.EvaluateBlockStmt(state, ifStmt.Thn) :
            InsertIf(ifStmt.Els as dfy.IfStmt, ExtractGuard(ifStmt.Els), state);
        }
        foreach (var statement in GenerateIfStmt(ifStmt, resultExpression, ifStmtEnum, elseStmtEnum, state)) {
          yield return statement;
        }
      }
    }

    private IEnumerable<ProofState> GenerateIfStmt(dfy.IfStmt original, Expression guard,
      IEnumerable<ProofState> ifStmtEnum, IEnumerable<ProofState> elseStmtEnum,
      ProofState state) {
      var cl = new Cloner();

      foreach (var @if in ifStmtEnum) {
        var statementList = @if.GetGeneratedCode();
        var ifBody = new BlockStmt(original.Thn.Tok, original.Thn.EndTok, statementList);
        if (elseStmtEnum != null) {
          foreach (var @else in elseStmtEnum) {
            var elseList = @else.GetGeneratedCode();
            Statement elseBody = null;
            // if original body was a plain else block
            if (original.Els is BlockStmt)
              elseBody = new BlockStmt(original.Els.Tok, original.Thn.EndTok, elseList);
            else // otherwise it was a 'else if' and the solution list should only contain one if stmt
              elseBody = elseList[0];
            var c = state.Copy();

            c.AddStatement(new dfy.IfStmt(original.Tok, original.EndTok, original.IsExistentialGuard,
              cl.CloneExpr(guard), ifBody, elseBody));
            yield return c;
          }
        } else {
          var c = state.Copy();
          c.AddStatement(new dfy.IfStmt(original.Tok, original.EndTok, original.IsExistentialGuard, cl.CloneExpr(guard),
            ifBody, null));
          yield return c;
        }
      }
    }
  }
}
*/
