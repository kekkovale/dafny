using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Boogie;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny.Atomic{
  class TacticInv : Atomic{
    public override string Signature => "invariant";
    public override int ArgsCount => 1;

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state){

      List<Expression> callArguments;
      IVariable lv;
      InitArgs(state, statement, out lv, out callArguments);

      var expr = SimpTaticExpr.SimpTacExpr(state, callArguments[0]);
        if (expr is Expression){
          var dest_stmt = new AssumeStmt(null, null, expr, null);
          var state0 = state.Copy();
          state0.AddStatement(dest_stmt);

          yield return state0;
        }
    }
  }
}
