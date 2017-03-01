using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Microsoft.Dafny.Tacny.Language {
  class OrChoiceStmt : TacticFrameCtrl{

    // All has to be * if it is non-deterministic - we could change to only one?
    [Pure]
    public override bool MatchStmt(Statement statement, ProofState state) {
      Contract.Requires(statement != null);
      if(statement is Dafny.IfStmt) {
        var ifstmt = statement as Dafny.IfStmt;
        if(ifstmt.Guard == null)
          return true;
        else
          return false;
      }
      if(statement is AlternativeStmt) {
        var ifstmt = statement as AlternativeStmt;
        // todo: should we allow just a single wildcard (*) or require that all are as we do now?
        foreach(GuardedAlternative a in ifstmt.Alternatives) {
          if(!(a.Guard is WildcardExpr))
            return false;
        }
        return true;
      }
      return false;
    }



    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {
      Contract.Requires(statement != null);
      Contract.Requires(MatchStmt(statement, state0));
      //Contract.Requires(statement is TacnyCasesBlockStmt);

      bool partial = true || state0.IsCurFramePartial();

      List<BlockStmt> choices = new List<BlockStmt>();

      if(statement is Dafny.IfStmt) {
        var ifstmt = statement as Dafny.IfStmt;
        if(ifstmt.Thn != null)
          choices.Add(ifstmt.Thn as BlockStmt);
        if(ifstmt.Els != null)
          choices.Add(ifstmt.Els as BlockStmt);
      }
      if(statement is AlternativeStmt) {
        var ifstmt = statement as AlternativeStmt;
        foreach(GuardedAlternative a in ifstmt.Alternatives) {
          if(a.Body != null && a.Body.Count != 0)
            choices.Add(new BlockStmt(a.Body.First().Tok, a.Body.Last().EndTok, a.Body));
        }
      }
      ProofState state = null;
      foreach(BlockStmt choice in choices) {
        state = state0.Copy();
        var orChoice = this.Copy();
        orChoice.InitBasicFrameCtrl(choice.Body, partial, null);
        state.AddNewFrame(orChoice);
        yield return state;
      }
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var statement = GetStmt();
      return Interpreter.EvalStmt(statement, state0);
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps){
      return _rawCodeList.Count == 1;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}
