using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny.Language {
  public class TBlockStmt : TacticFrameCtrl {
    public override bool MatchStmt(Statement stmt, ProofState state) {
      var us = stmt as BlockStmt;
      return us != null ;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {
      var state = state0.Copy();
      var blockStmt = statement as Dafny.BlockStmt;
      if(blockStmt != null) {
        var frameCtrl = new DefaultTacticFrameCtrl();
        frameCtrl.InitBasicFrameCtrl(blockStmt.Body, state0.IsCurFramePartial(), null);
        state.AddNewFrame(frameCtrl);
        yield return state;
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