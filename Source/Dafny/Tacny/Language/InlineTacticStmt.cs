using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny.Language
{
  public class InlineTacticStmt : TacticFrameCtrl
  {
    public override bool MatchStmt(Statement stmt, ProofState state) {
      var us = stmt as InlineTacticBlockStmt;
      return us != null;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state) {
      var inline = statement as InlineTacticBlockStmt;
      //check whether the tactic name is fresh
      bool isFresh = !(state.Tactics.ContainsKey(inline.name) || state.Members.ContainsKey(inline.name));
      if (isFresh) {
        var frameCtrl = new DefaultTacticFrameCtrl();
        frameCtrl.InitBasicFrameCtrl(inline.Body.Body, state.IsCurFramePartial(), null);
        state.AddNewFrame(frameCtrl);
        yield return state;
      } else {
        CompoundErrorInformation.AddErrorInfo(state, "Duplicated inline tactic name.");
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