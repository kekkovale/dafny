using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.EAtomic {
  class LoopGuard: EAtomic {
    public override string Signature => "loop_guard";
    public override int ArgsCount => 0;

    public override IEnumerable<object> Generate(Expression expression, ProofState proofState){
      throw new NotImplementedException();
    }

  }
}
