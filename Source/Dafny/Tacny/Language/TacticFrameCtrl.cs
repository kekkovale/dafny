using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.Dafny.Tacny.Language{
  abstract class TacticFrameCtrl{
    public abstract bool IsPartial { get; }
    public abstract string Signature { get; }

    public abstract IEnumerable<ProofState> EvalStep(Statement statement, ProofState state0);
    public abstract IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0);
    public abstract bool EvalTerminated(List<List<Statement>> raw);
    public abstract List<Statement> Assemble(List<List<Statement>> raw);


    public static bool IsFlowControl(Statement stmt){
      return stmt is IfStmt || stmt is WhileStmt || stmt is TacnyCasesBlockStmt || stmt is AlternativeStmt;
    }


    public static bool IsFlowControlFrame(ProofState state){
      var typ = state.GetCurFrameTyp();
      //more control frame should be added here
      return (typ == "tmatch") || (typ == "orchoice");
    }

  }

  class DefaultTacticFrameCtrl : TacticFrameCtrl {
      public override string Signature => "default";
      public override bool IsPartial => false;

      public override IEnumerable<ProofState> EvalStep(Statement statement, ProofState state0){
      return Interpreter.EvalStmt(statement, state0);
    }

    override public IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      // not supposed to be called, for the deault frame, no need to init
      Contract.Assert(false);
      return null;
    }

    override public bool EvalTerminated(List<List<Statement>> raw){
        return true;
    }

    override public List<Statement> Assemble(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
