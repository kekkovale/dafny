using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.Dafny.Tacny.Atomic
{
  /// <summary>
  ///   Abstract class for Atomic Statement
  /// </summary>
  public abstract class Atomic
  {
    public abstract string Signature { get; }
    public abstract int ArgsCount { get; }

    /// <summary>
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public abstract IEnumerable<ProofState> Generate(Statement statement, ProofState state);

    protected static List<Expression> GetCallArguments(UpdateStmt us) {
      Contract.Requires(us != null);
      var er = (ExprRhs)us.Rhss[0];
      return ((ApplySuffix)er.Expr).Args;
    }

    protected static void InitArgs(ProofState state, Statement st, out List<Expression> callArguments) {
      Contract.Requires(st != null);
      Contract.Requires(state != null);

      callArguments = null;

      if (st is UpdateStmt) {
        var us = st as UpdateStmt;
        callArguments = GetCallArguments(us);
      }
    }
  }
}