using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Microsoft.Dafny.Tacny.EAtomic {

  /// <summary>
  ///   Abstact class for Atomic Expressions
  /// </summary>
  [ContractClass(typeof(EAtomicContract))]
  public abstract class EAtomic : BaseTactic {
    public static List<string> EATomicSigList;

    public static bool IsEAtomicSig(string sig){
      if (EATomicSigList == null) return false;
      return EATomicSigList.Exists(sig.Equals);
    }

    public static void InitEAtomicSigList() {
      EATomicSigList = new List<string>();
      var types =
            Assembly.GetAssembly(typeof(EAtomic))
              .GetTypes()
              .Where(t => t.IsSubclassOf(typeof(EAtomic)));
      foreach(var eType in types) {
        var eatomInst = Activator.CreateInstance(eType) as EAtomic;
        EATomicSigList.Add(eatomInst.Signature);
      }
    }

    public abstract override string Signature { get; }

    // TypeOf (Expression expression, ProofState proofState); 
    // next step will be to implement proer typing, perhaps in F#

    /// <summary>
    ///   Common entry point for each atomic
    /// </summary>
    /// <param name="expression">Expression to be resolved</param>
    /// <param name="proofState">Current tactic ProofState</param>
    /// <returns>Lazily returns generated objects one at a time</returns>
    public abstract object Generate(Expression expression, ProofState proofState);
  }

  [ContractClassFor(typeof(EAtomic))]
  public class EAtomicContract : EAtomic {
    public override string Signature { get; }
    public override int ArgsCount { get; }

    public override object Generate(Expression expression, ProofState proofState) {
      Contract.Requires(expression != null);
      Contract.Requires(proofState != null);
      return null;
    }
  }
}