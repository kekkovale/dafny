using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Boogie;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny.Language
{
  class TAssert : TacticFrameCtrl
  {
    private bool _pass = false;
    public override bool MatchStmt(Statement stmt, ProofState state) {
      return stmt is TacticAssertStmt;
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {

      var dummyBody = new List<Statement> { statement };
      InitBasicFrameCtrl(dummyBody, false, null);
      state0.AddNewFrame(this);

      var assertFrame = new DefaultTacticFrameCtrl();
      assertFrame.InitBasicFrameCtrl(dummyBody, false, null);
      assertFrame.IncCounter();
      state0.AddNewFrame(assertFrame);

      var st = SimpTacticExpr.SimpTacExpr(state0, (statement as TacticAssertStmt).Expr);
      // insert the simplified assert, followed by assume false so that the prover don't need to worry about the 
      // following vcs.

      var asserts = new List<Statement>();
      asserts.Add(
       new AssertStmt(new Token(Interpreter.TacnyCodeTokLine, 0) { val = "assert" },
         new Token(Interpreter.TacnyCodeTokLine, 0) { val = ";" }, st, null, null));
      asserts.Add(
      new AssumeStmt(new Token(Interpreter.TacnyCodeTokLine, 0) { val = "assume" },
       new Token(Interpreter.TacnyCodeTokLine, 0) { val = ";" },
       new Microsoft.Dafny.LiteralExpr(new Token(Interpreter.TacnyCodeTokLine, 0) { val = "false" }, false),
       null));

      state0.AddStatements(asserts);

      state0.NeedVerify = true;

      yield return state0;
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0)
    {
      _pass = true;
      RawCodeList = new List<List<Statement>>();
      GeneratedCode = new List<Statement>();
      //inc counter to finish the evaluation of the current fram, note that there is only one statement.
      // so one is enough
      IncCounter();
      yield return state0;
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState state) {
      return _pass;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw) {
      return raw.SelectMany(x => x).ToList();
    }
  }
}
