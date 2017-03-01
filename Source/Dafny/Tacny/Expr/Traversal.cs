using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.Expr {

  class Traversal {

    public static void TraverseStatement(Statement stmt, Action<Statement> fstmt, Action<Expression> fexpr) {
      Contract.Requires(stmt != null);
      fstmt(stmt);
      foreach (Expression e in stmt.SubExpressions) {
        TraverseExpression(e,fexpr);
      }
      foreach (Statement s in stmt.SubStatements) {
        TraverseStatement(stmt, fstmt, fexpr);
      }
    }

    public static void TraverseExpression(Expression expr, Action<Expression> fexpr) {
      Contract.Requires(expr != null);
      fexpr(expr);
      foreach (Expression e in expr.SubExpressions) {
        fexpr(e);
      }
    }

  }

  class AllVars {

    /*
      * Assumes variables only by assignment
      * Bound only by 
    */

    private List<String> _vars;

    private void ExprVars(Expression e) {
      Contract.Requires(_vars != null);
      Contract.Requires(e != null);

      if (e is ComprehensionExpr) {
        ComprehensionExpr c = e as ComprehensionExpr;
        foreach (BoundVar bv in c.BoundVars) {
          _vars.Add(bv.Name);
        }
      }
    }

    private void StmtVars(Statement s) {
      Contract.Requires(_vars != null);
      Contract.Requires(s != null);

      
    }

    // can also get a ref to a list as argument
    public static void DeclaredVars(Statement stmt,ref List<String> ls) {
      Contract.Requires(ls != null);
       AllVars v = new AllVars();
       v._vars = ls;
       Traversal.TraverseStatement(stmt,v.StmtVars,v.ExprVars);
      // is this necessary?
       ls = v._vars;
    }
  }
}
