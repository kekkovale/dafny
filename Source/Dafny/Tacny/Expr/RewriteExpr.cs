using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.Expr {
  class RenameVar : Cloner {

    private Dictionary<String, String> _renames;

    public RenameVar() : base() {
      _renames = new Dictionary<String, String>();
    }

    public void AddRename(String before, String after) {
      _renames.Add(before,after);
    }

    /*
    public override BoundVar CloneBoundVar(BoundVar bv) { 
      var bvNew = new BoundVar(Tok(bv.tok), "dummy", CloneType(bv.Type));
      bvNew.IsGhost = bv.IsGhost;
      return bvNew;
    }
    */

    public override NameSegment CloneNameSegment(Expression expr) {
      var e = (NameSegment) expr;
      String nm, name;
      if (_renames.TryGetValue(e.Name, out nm))
        name = nm;
      else {
        name = e.Name;
      }
      return new NameSegment(Tok(e.tok), name,
        e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
    }
  }

}