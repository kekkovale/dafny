using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny.Language {
  class WhileCtrlStmt : TacticFrameCtrl{
    private Expression guard;
    private List<Statement> body;
    //default partial: true

    public new bool IsEvaluated => false;

    public override bool MatchStmt(Statement stmt, ProofState state){
      return stmt is WhileStmt;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      Contract.Requires(statement is WhileStmt);
      var whileStmt = statement as WhileStmt;

      var tryEval = SimpTacticExpr.EvalTacticExpr(state0, whileStmt.Guard) as BooleanRet;

      if(tryEval == null) {
        var state = state0.Copy();
        var st = SimpTacticExpr.SimpTacExpr(state, statement);
        state.NeedVerify = true;
        state.AddStatement(st);
        yield return state;
        yield break;
      } else {
        var state = state0.Copy();
        var whileCtrl = this.Copy();

        whileCtrl.guard = whileStmt.Guard;
        whileCtrl.body = whileStmt.Body.Body;

        if (tryEval.value) {
          // insert the control frame
          whileCtrl.IsPartial = true;
          var dummyBody = new List<Statement>();
          dummyBody.Add(whileStmt);
          whileCtrl.InitBasicFrameCtrl(dummyBody, null);
          state.AddNewFrame(whileCtrl);

          //insert the body frame
          var bodyFrame = new DefaultTacticFrameCtrl();
          bodyFrame.InitBasicFrameCtrl(whileCtrl.body, null);
          bodyFrame.IsPartial = whileCtrl.IsPartial;
          state.AddNewFrame(bodyFrame);
        }

        yield return state;
      }
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var state = state0.Copy();

      var tryEval = SimpTacticExpr.EvalTacticExpr(state, guard) as BooleanRet;
      Contract.Assert(tryEval != null);

      if(tryEval.value){
        //insert the body frame
        var bodyFrame = new DefaultTacticFrameCtrl();
        bodyFrame.InitBasicFrameCtrl(body, null);
        bodyFrame.IsPartial = state.IsCurFramePartial();
        state.AddNewFrame(bodyFrame);
      }
      else{
        state.NeedVerify = true;
      }
      yield return state;
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState state){
      var tryEval = SimpTacticExpr.EvalTacticExpr(state, guard) as BooleanRet;
      Contract.Assert(tryEval != null);
      return !tryEval.value;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
