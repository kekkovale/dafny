using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny.Language {
  public class TryBlockStmt : TacticFrameCtrl {
    public override bool MatchStmt(Statement stmt, ProofState state){
      var us = stmt as TacticTryBlockStmt;
      return us != null;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {
      var tryBlockStmt = statement as TacticTryBlockStmt;
      if(tryBlockStmt != null) {
        var frameCtrl = new DefaultTacticFrameCtrl();
        frameCtrl.InitBasicFrameCtrl(tryBlockStmt.Body.Body, state0.IsCurFramePartial(), null);
        var state = state0.Copy();
        state.AddNewFrame(frameCtrl);
        yield return state;
        yield return state0;
      }
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0) {
      var statement = GetStmt();
      return Interpreter.EvalStmt(statement, state0);
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps) {
      return childFrameRes;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw) {
      return raw.SelectMany(x => x).ToList();
    }
  }
}