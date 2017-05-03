using System;
using System.Collections.Generic;

namespace Microsoft.Dafny.Tacny.EAtomic {
  class LoopGuard: EAtomic {
    public override string Signature => "invariant";
    public override int ArgsCount => 0;

    public override Expression Generate(Expression expression, ProofState proofState){
      throw new NotImplementedException();
    }

  }
}
