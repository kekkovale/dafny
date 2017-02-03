using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Boogie;


namespace Microsoft.Dafny.Tacny.Language {

  class ForallStmt : TacticFrameCtrl {

    private TacnyForallStmt _stmt;
    private List<BoundVar> _vars;
    private Expression _range;
    private List<MaybeFreeExpression> _ens;


    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0) {
      Contract.Requires(statement != null);
      Contract.Requires(statement is TacnyForallStmt);

      _stmt = statement as TacnyForallStmt;
 
      Contract.Assert(_stmt != null);

      // Version 1: ignore range and multiple bounds
      // i.e. ignore implication

      var e = _stmt.Spec as ForallExpr;
      _vars = e.BoundVars;
      //fixme
      _range = new LiteralExpr(_stmt.Tok, true);
      var t = new MaybeFreeExpression(e.Term);
      var l = new List<MaybeFreeExpression>();
      l.Add(t);
      _ens = l;

      this.InitBasicFrameCtrl(new List<Statement>(),null);

      var state = state0.Copy();
      state.AddNewFrame(this);

      var bodyFrame = new DefaultTacticFrameCtrl();
      bodyFrame.InitBasicFrameCtrl(_stmt.Body.Body, null);
      bodyFrame.IsPartial = this.IsPartial;
      state.AddNewFrame(bodyFrame);

      yield return state;
    }

    // not sure about this?
    public override IEnumerable<ProofState> EvalStep(ProofState state0) {
      var statement = GetStmt();
      return Interpreter.EvalStmt(statement, state0);
    }

    // not sure about this?
    public override bool EvalTerminated(bool childFrameRes, ProofState state) {
      return _rawCodeList.Count == 1;
    }

    public override List<Statement> AssembleStmts(List<List<Statement>> raw) {
      Contract.Assume(raw.Count == 1);

      IToken start = TokenGenerator.NextToken(_stmt.Tok, _stmt.Tok);
      IToken end = TokenGenerator.NextToken(_stmt.EndTok, _stmt.EndTok);

      var stmts = (List<Statement>)raw.First();
      //FIXME: update tokens
      var body = new Dafny.BlockStmt(start, end, stmts);
      var stmt = new Dafny.ForallStmt(start, end, _vars, null, _range, _ens, body);
      var list = new List<Statement>();
      list.Add(stmt);
      return list;
    }















  /*
   *  TODO: generalise: this could also be a meta-level variable or tactic application
   *  FIXME: doesn't have to be an implication
   */

  [Pure]
    public static bool IsForallShape(Expression e) {
      Contract.Requires(e != null);

      if (e is Microsoft.Dafny.ForallExpr) {
        var fall = e as Microsoft.Dafny.ForallExpr;
        if (fall.LogicalBody() is BinaryExpr) {
          var body = fall.LogicalBody() as BinaryExpr;
          if (body?.Op == BinaryExpr.Opcode.Imp) {
            return true;
          }
          else {
            return false;
          }
        }
      }
      return false;
    }

    public static IEnumerable<ProofState> EvalNext(Statement statement, ProofState state0) {

      yield break;
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

    public override bool MatchStmt(Statement stmt, ProofState state) {
      Contract.Requires(stmt != null);

      return stmt is TacnyForallStmt;
    }

  }

}

