using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.EAtomic {
  class PostCond : EAtomic {
    public override string Signature => "post_conds";
    public override int ArgsCount => 0;

    public override IEnumerable<object> Generate(Expression expression, ProofState proofState) {
      var posts = new List<Expression>();

      foreach(var item in (proofState.TargetMethod as Method).Ens) {
        posts.Add(item.E);
      }

      yield return posts;
    }
  }
}

