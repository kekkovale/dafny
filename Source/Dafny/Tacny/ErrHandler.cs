using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Boogie;
using System.Diagnostics.Contracts;

namespace Microsoft.Dafny.Tacny
{
  public class ErrHandler
  {
    private List<IToken> pos; // a list tokens keeps track of application location

    public ErrHandler() { pos = new List<IToken>(); }
    public void Push(IToken tok) {
      pos.Add(tok);
    }
    public IToken Pop() {
      //Contract.Requires(pos != null && pos.Count > 0);
      var idx = pos.Count - 1;
      var tok = pos[idx];
      pos.RemoveAt(idx);
      return tok;
    }


  }
}
