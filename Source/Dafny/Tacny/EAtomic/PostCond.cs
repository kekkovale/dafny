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

      //need to get postconds from unresolved prog
      var prog = proofState.GetDafnyProgram();

      var tld = prog.DefaultModuleDef.TopLevelDecls.FirstOrDefault(x => x.Name == proofState.ActiveClass.Name) as ClassDecl;
      var member = tld.Members.FirstOrDefault(x => x.Name == proofState.TargetMethod.Name) as Method;

      var posts = new List<Expression>();
      foreach(var post in member.Ens) {
        posts.Add(post.E);
      }
      yield return posts;


    }
  }
}

