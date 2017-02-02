using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny.Atomic{
  class TInvatiant : Atomic{
    public override string Signature => "invariant";
    public override int ArgsCount => 1;

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state){

      List<Expression> callArguments;
      IVariable lv;
      InitArgs(state, statement, out lv, out callArguments);

      var enumrator = Interpreter.EvalTacnyExpression(state, callArguments[0]).GetEnumerator();

      while (enumrator.MoveNext()){
        if (enumrator.Current is Expression){
          var exp = enumrator.Current as Expression;
          var dest_stmt = new AssumeStmt(null, null, exp, null);
          var state0 = state.Copy();
          state0.AddStatement(dest_stmt);

          yield return state0;
        }
      }
    }
  }
}
