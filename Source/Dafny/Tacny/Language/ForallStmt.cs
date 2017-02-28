using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Boogie;
using Microsoft.Dafny.Tacny.Expr;


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

      // do basic simplification
      // maybe do a check and throw an error instead?  
      // fixme: error returns null!
      var e = (ForallExpr) SimpTacticExpr.SimpTacExpr(state0, _stmt.Spec);
      // var e = _stmt.Spec as ForallExpr;
      RenameVar rn = new RenameVar();

      if (_stmt.Attributes.Name.Equals("vars")) {
        var attrs = _stmt.Attributes.Args;
        for (int i = 0; i < attrs.Count; i++) {
          // todo: should really report an errors if first condition does not hold
          if (attrs[i] is NameSegment && i < e.BoundVars.Count) {
            NameSegment ns = attrs[i] as NameSegment;
            rn.AddRename(e.BoundVars[i].Name, ns.Name);
          } // else we should have an error
          _vars = new List<BoundVar>();
          foreach (BoundVar bv in e.BoundVars) {
            _vars.Add(rn.CloneBoundVar(bv));
          }
        }

      }else {
        _vars = e.BoundVars;
      }


      // we could even break  _ens into a set of all conjunctions?
      // what about forall x (forall y) x
      if (e.Term is BinaryExpr && (((BinaryExpr) e.Term).Op.Equals(BinaryExpr.Opcode.Imp))) {
        var be = e.Term as BinaryExpr;
        _range = rn.CloneExpr(be.E0);
        var t = new MaybeFreeExpression(rn.CloneExpr(be.E1));
        var l = new List<MaybeFreeExpression>();
        l.Add(t);
        _ens = l;
      }else {
        _range = new LiteralExpr(_stmt.Tok, true);
        var t = new MaybeFreeExpression(rn.CloneExpr(e.Term));
        var l = new List<MaybeFreeExpression>();
        l.Add(t);
        _ens = l;
      }

      // Note that we do not need to rename variables in the body (unless the variables in vars is changed)
      
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


    // List functions should be moved somewhere else...

    [Pure]
    public String NextString(String v) {
      char last = v.ElementAt(v.Length - 1);
      if (Char.IsNumber(last)) {
        double n = Char.GetNumericValue(last) + 1;
        return v.Substring(0, v.Length - 1) + n;
      } else {
        return v + "0";
      }
    }
    
    [Pure]
    public String GenFreeVar(String var, ICollection<String> allvars) {
      while(allvars.Contains(var))
        var = NextString(var);
      return var;
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

    private void boundVars(ProofState state,Expression e) {
      var varDict = state.GetAllDafnyVars();
     

      Zipper zip = new Zipper(e);
    
      Func<Expression, ISet<string>, ISet<string>> fv = (expr, set) => {
        if (expr is NameSegment) {
          var en = (NameSegment)expr;
          set.Add(en.Name);
        }
       return set;
      };
      HashSet<string> hs = new HashSet<string>();
      var els = zip.Fold(fv, hs);


    }

    public override bool MatchStmt(Statement stmt, ProofState state) {
      Contract.Requires(stmt != null);

      return stmt is TacnyForallStmt;
    }

  }

}

