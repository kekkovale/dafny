using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.Dafny.Tacny.Language{
  public abstract class TacticFrameCtrl{
    public abstract bool IsPartial { get; }
    public abstract string Signature { get; }

    public abstract bool MatchStmt(Statement stmt);
    public abstract IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0);
    public abstract IEnumerable<ProofState> EvalStep(Statement statement, ProofState state0);
    public abstract bool EvalTerminated(List<List<Statement>> raw, bool childFrameRes);
    public abstract List<Statement> Assemble(List<List<Statement>> raw);

 /*
    public static bool IsFlowControlFrame(ProofState state){
      var typ = state.GetCurFrameTyp();
      //more control frame should be added here
      return (typ == "tmatch") || (typ == "orchoice");
    }
 */
  }

  class DefaultTacticFrameCtrl : TacticFrameCtrl {
      public override string Signature => "default";
      public override bool IsPartial => false;

    public override bool MatchStmt(Statement stmt){
      /* the default one always returns false, as we don't need this to judge if a stmt belongs to this type.
       * One stmt would be considered as the default one when all other matches fail. */
      return false;
    }

    public override IEnumerable<ProofState> EvalStep(Statement statement, ProofState state0){
      return Interpreter.EvalStmt(statement, state0);
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      // not supposed to be called, for the deault frame, no need to init
      Contract.Assert(false);
      return null;
    }

    public override bool EvalTerminated(List<List<Statement>> raw, bool latestChildFrameRes) {
        return latestChildFrameRes;
    }

    public override  List<Statement> Assemble(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
