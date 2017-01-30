using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Boogie;


namespace Microsoft.Dafny.Tacny.Language {

  class ForallStmt {

    private TacnyForallStmt _stmt;
    private List<BoundVar> _vars;
    private Expression _range;
    private List<MaybeFreeExpression> _ens;

    /*
     *  TODO: generalise: this could also be a meta-level variable or tactic application
     *  FIXME: doesn't have to be an implication
     */
    [Pure]
    public static bool IsForallShape(Expression e) {
      Contract.Requires(e != null);

      if(e is Microsoft.Dafny.ForallExpr) {
        var fall = e as Microsoft.Dafny.ForallExpr;
        if(fall.LogicalBody() is BinaryExpr) {
          var body = fall.LogicalBody() as BinaryExpr;
          if(body?.Op == BinaryExpr.Opcode.Imp) {
            return true;
          } else {
            return false;
          }
        }
      }
      return false;
    }

    public static IEnumerable<ProofState> EvalNext(Statement statement, ProofState state0) {

      yield break;
    }

    public static IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {
      Contract.Requires(statement != null);
      Contract.Requires(statement is TacnyForallStmt);

      IToken newtok = TokenGenerator.NextToken();

      var stmt = statement as TacnyForallStmt;
      var e = stmt.Spec;
      var attr = stmt.Attributes;
      var nms = new List<String>();
      if(attr.Name == "") {
        var nms_args = attr.Args; // need to extract name SHould we fail otherwise?
        foreach(Expression ee in nms_args) {
          if(ee is StringLiteralExpr) {
            var st = ee as StringLiteralExpr;
            nms.Add(st.AsStringLiteral());
          }
        }
      }

      Contract.Assert(e != null && IsForallShape(e));

      // var body = new Microsoft.Dafny.ForallStmt();
      //q = new ForallExpr(x, bvars, range, body, attrs);
      //s = new ForallStmt(x, tok, bvars, attrs, range, ens, block);

      var state = state0.Copy();

      yield return state;
    }

    /*
     * Term work:
     *  - check free vars
     *  - instantiate var (possible rename)
     *  - rename all instances in program text (body)
     *  - 
     */

    public Dafny.ForallStmt GenerateForallStmt(Statement body) {
    //  Contract.Requires(_stmt != null);
      IToken start = TokenGenerator.NextToken(_stmt.Tok, _stmt.Tok);
      IToken end = TokenGenerator.NextToken(_stmt.EndTok, _stmt.EndTok);

      return new Dafny.ForallStmt(start, end, _vars, null, _range, _ens, body);
    }

    private int freeVars() {
      return 0;
    }

    private void instVar() {

    }
  }

}

