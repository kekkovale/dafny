using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny.Language {
  class WhileCtrlStmt : TacticFrameCtrl{
    private Expression _guard;
    private List<Statement> _body;
    //default partial: true

    public new bool IsEvaluated => false;

    public override bool MatchStmt(Statement stmt, ProofState state){
      return stmt is WhileStmt;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      Contract.Requires(statement is WhileStmt);
      var whileStmt = statement as WhileStmt;

      if (whileStmt != null)
      {
        var tryEval = RewriteExpr.UnfoldTacticProjection(state0, whileStmt.Guard) as BooleanRet;

        if(tryEval == null) {
          var state = state0.Copy();
          var st = RewriteExpr.SimpTacticExpr(state, statement);
          state.NeedVerify = true;
          state.AddStatement(st);
          yield return state;
          yield break;
        } else {
          var state = state0.Copy();
          var whileCtrl = this.Copy();

          whileCtrl._guard = whileStmt.Guard;
          whileCtrl._body = whileStmt.Body.Body;

          if (tryEval.Value) {
            // insert the control frame
            var dummyBody = new List<Statement> {whileStmt};
            whileCtrl.InitBasicFrameCtrl(dummyBody, true, null);
            state.AddNewFrame(whileCtrl);

            //insert the body frame
            var bodyFrame = new DefaultTacticFrameCtrl();
            bodyFrame.InitBasicFrameCtrl(whileCtrl._body, whileCtrl.IsPartial, null);
            state.AddNewFrame(bodyFrame);
          }

          yield return state;
        }
      }
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var state = state0.Copy();

      var tryEval = RewriteExpr.UnfoldTacticProjection(state, _guard) as BooleanRet;
      Contract.Assert(tryEval != null);

      if(tryEval.Value){
        //insert the body frame
        var bodyFrame = new DefaultTacticFrameCtrl();
        bodyFrame.InitBasicFrameCtrl(_body, state.IsCurFramePartial(), null);
        state.AddNewFrame(bodyFrame);
      }
      else{
        state.NeedVerify = true;
      }
      yield return state;
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState state){
      var tryEval = RewriteExpr.UnfoldTacticProjection(state, _guard) as BooleanRet;
      Contract.Assert(tryEval != null);
      return !tryEval.Value;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
