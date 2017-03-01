using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dafny.Tacny.Language{
  public class TacAppStmt : TacticFrameCtrl{
    public override bool MatchStmt(Statement stmt, ProofState state){
      return stmt is UpdateStmt && state.IsTacticCall(stmt as UpdateStmt);
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      var state = state0.Copy();
      var tacApsStmt = statement as UpdateStmt;
      var aps = ((ExprRhs)tacApsStmt.Rhss[0]).Expr as ApplySuffix;
      var tactic = state.GetTactic(aps) as Tactic;

      var frameCtrl = new DefaultTacticFrameCtrl();
      frameCtrl.InitBasicFrameCtrl(tactic.Body.Body, true, tacApsStmt.Rhss[0].Attributes, tactic);
      state.AddNewFrame(frameCtrl);

      if(aps.Args.Count != tactic.Ins.Count)
        state.Reporter.Error(MessageSource.Tacny, tacApsStmt.Tok,
          $"Wrong number of method arguments (got {aps.Args.Count}, expected {tactic.Ins.Count})");

      for (int index = 0; index < aps.Args.Count; index++){
        var arg = aps.Args[index];

        if (arg is Microsoft.Dafny.NameSegment){
          var name = ((Microsoft.Dafny.NameSegment) arg).Name;
          if (state.ContainTVal(name))
            // in the case that referring to an exisiting tvar, dereference it
            arg = state.GetTVarValue(name) as Expression;
          else{
            state.Reporter.Error(MessageSource.Tacny, tacApsStmt.Tok,
              $"Fail to dereferenen argument({name})");
          }
        }
        state.AddTacnyVar(tactic.Ins[index].Name, arg);
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