using System.Linq;

namespace Microsoft.Dafny.Tacny.EAtomic {
  class Params : EAtomic {
    public override string Signature => "params";
    public override int ArgsCount => 0;

    // parameters can be checked by combine the type Formal and the InParam attribute
    private static bool IsParam(ProofState.VariableData var)
    {
      var formal = var.Variable as Formal;
      if(formal != null) {
        var v = formal;
        return v.InParam;
      } else
        return false;
    }

    /// <summary>
    ///  Project the parameters of the calling method/function
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="proofState"></param>
    /// <returns></returns>
    public override object Generate(Expression expression, ProofState proofState) {
      var vars = proofState.GetAllDafnyVars().Values.ToList().Where(IsParam);
      return vars.Select(x => x.Variable).ToList();
    }
  }
}
