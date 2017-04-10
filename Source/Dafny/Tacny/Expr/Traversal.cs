using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;


namespace Microsoft.Dafny.Tacny.Expr {

  class Traversal {

    public static void TraverseStatement(Statement stmt, Action<Statement> fstmt, Action<Expression> fexpr) {
      Contract.Requires(stmt != null);
      fstmt(stmt);
      foreach (Expression e in stmt.SubExpressions) {
        TraverseExpression(e,fexpr);
      }
      foreach (Statement s in stmt.SubStatements) {
        TraverseStatement(s, fstmt, fexpr);
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

      var expr = e as ComprehensionExpr;
      if (expr != null) {
        ComprehensionExpr c = expr;
        foreach (BoundVar bv in c.BoundVars) {
          _vars.Add(bv.Name);
        }
      }
    }

    private void StmtVars(Statement stmt) {
      Contract.Requires(_vars != null);
      Contract.Requires(stmt != null);

      var declStmt = stmt as VarDeclStmt;
      if (declStmt != null) {
        var s = declStmt;
        foreach (LocalVariable l in s.Locals) {
          _vars.Add(l.Name);
        }
      }
      // FIXME: t this should really be handled by SubExpressions function in AST but doesn't seem to be correct
      var updateStmt = stmt as UpdateStmt;
      if (updateStmt != null) {
        var s = updateStmt;
        foreach (AssignmentRhs rhs in s.Rhss)
          foreach (Expression e in rhs.SubExpressions)
            ExprVars(e);
      }
    }

    

    // can also get a ref to a list as argument
    public static void DeclaredVars(Statement stmt,ref List<String> ls) {
      Contract.Requires(ls != null);
      AllVars v = new AllVars {_vars = ls};
      Traversal.TraverseStatement(stmt,v.StmtVars,v.ExprVars);
      // is this necessary?
       ls = v._vars;
    }
  }
}
