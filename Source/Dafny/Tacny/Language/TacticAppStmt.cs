using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny.Language{
  public class TacticAppStmt : TacticFrameCtrl{
    
    public override bool MatchStmt(Statement stmt, ProofState state)
    {
      var us = stmt as UpdateStmt;
      return (us != null && state.IsTacticCall(us));
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      var state = state0.Copy();
      var tacApsStmt = statement as UpdateStmt;
      if (tacApsStmt != null)
      {
        var aps = ((ExprRhs)tacApsStmt.Rhss[0]).Expr as ApplySuffix;
    
        var tactic = state.GetTactic(aps) as Tactic;

        var frameCtrl = new DefaultTacticFrameCtrl();
        if (tactic != null)
        {
          //unfolding preconditions
          List<Statement> body = new List<Statement>();
          if (tactic.Req != null) {
            foreach (var expr in tactic.Req) {
              body.Add(
                new TacticAssertStmt(
                  new Token(Interpreter.TacticCodeTokLine, 0) { val = "tassert" },
                  new Token(Interpreter.TacticCodeTokLine, 0) { val = ";" },
                  expr.E,
                  null, false));
            }
          }
          body.AddRange(tactic.Body.Body);
          //unfolding postcondition
          if (tactic.Ens != null) {
            foreach (var expr in tactic.Ens) {
              body.Add(
                new TacticAssertStmt(
                  new Token(Interpreter.TacticCodeTokLine, 0) { val = "tassert" },
                  new Token(Interpreter.TacticCodeTokLine, 0) { val = ";" },
                  expr.E,
                  null, false));
            }
          }

          frameCtrl.InitBasicFrameCtrl(body, true, tacApsStmt.Rhss[0].Attributes, tactic);
          state.AddNewFrame(frameCtrl);

          if(aps != null && aps.Args.Count != tactic.Ins.Count)
            state.GetErrHandler().Reporter.Error(MessageSource.Tactic, tacApsStmt.Tok,
              $"Wrong number of method arguments (got {aps.Args.Count}, expected {tactic.Ins.Count})");

          for (var index = 0; index < aps.Args.Count; index++){
            var arg = aps.Args[index];

            var segment = arg as NameSegment;
            if (segment != null){
              var name = segment.Name;
              if (state.ContainTVal(name))
                // in the case that referring to an exisiting tvar, dereference it
                arg = state.GetTVarValue(name) as Expression;
              else{
                state.GetErrHandler().Reporter.Error(MessageSource.Tactic, tacApsStmt.Tok,
                  $"Fail to dereferenen argument({name})");
              }
            }
            state.AddTacnyVar(tactic.Ins[index].Name, arg);
          }
        }
      }
      yield return state;
    }

    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var statement = GetStmt();
      return Interpreter.EvalStmt(statement, state0);
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps){
      return childFrameRes;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      return raw.SelectMany(x => x).ToList();
    }
  }
}