﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny;


// Project the all dafny variables in the current scope of the calling method/function

namespace Microsoft.Dafny.Tacny.EAtomic { 
  class Variables : EAtomic {

    public override string Signature => "variables";
    public override int ArgsCount => 0;

    public override object Generate(Expression expression, ProofState proofState) {
      var vars = proofState.GetAllDafnyVars().Values.ToList();
      return vars.Select(x => x.Variable).ToList();
    }
  }
}
