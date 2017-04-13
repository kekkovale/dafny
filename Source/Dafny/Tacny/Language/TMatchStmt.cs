//YUHUI: this implemntation is over complicated, use raw[0][0] implicitly as staus data, need to be simplified
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Boogie;


namespace Microsoft.Dafny.Tacny.Language {
  class TMatchStmt : TacticFrameCtrl {


    /*
    private List<string> _names;
    private string PopCaseName(){

      if (_names.Count > 0){
        var ret = _names[0];
        _names.Remove(ret);
        return ret;
      }
      else
        return null;
    }

    public static List<string> ParseDefaultCasesNames(Statement stmt) {

      List<string> n = new List<string>();
      if (stmt.Attributes != null) { 
      if (stmt.Attributes.Name == "vars"){
        foreach (Expression x in stmt.Attributes.Args){
          if (x is NameSegment){
            var y = x as NameSegment;
            n.Add(y.Name);
          }
        }
      }
    }
      return n;
    }
    
    public Match(){
      _names = new List<string>();
    }
    
    public Match(Statement stmt) {
      _names = ParseDefaultCasesNames(stmt);
    }
    */
    public new bool IsEvaluated => false;
    public override bool MatchStmt(Statement stmt, ProofState state){
      return stmt is TacticCasesBlockStmt;
    }

    public override bool EvalTerminated(bool childFrameRes, ProofState ps)
    {
      //raw[0] is the match case stmt with assume false for each case
      //raw[1..] the actual code to bt inserted in the case statement

      var matchStmt = RawCodeList[0][0] as MatchStmt;
      return matchStmt != null && matchStmt.Cases.Count + 1== RawCodeList.Count;
    }

     public override List<Statement> AssembleStmts(List<List<Statement>> raw){
      //Contract.Requires(IsTerminated(raw));

      var matchStmt = raw[0][0] as MatchStmt;

      for (int i = 1; i < raw.Count; i++){
        if (matchStmt != null)
        {
          matchStmt.Cases[i-1].Body.Clear();
          matchStmt.Cases[i - 1].Body.AddRange(raw[i]);
        }
      }
       var ret = new List<Statement> {matchStmt};
       return ret;
    }

    internal int GetNthCaseIdx(List<List<Statement>> raw) {
      Contract.Requires(raw != null);
      Contract.Requires(raw.Count > 0);
      Contract.Requires(raw[0] != null && raw[0].Count == 1 && raw[0][0] is MatchStmt);
      return raw.Count - 1;

    }
    public override IEnumerable<ProofState> EvalStep(ProofState state0){
      var state = state0.Copy();

      var stmt = GetStmt() as TacticCasesBlockStmt;
      var framectrl = new DefaultTacticFrameCtrl();
      if (stmt != null) framectrl.InitBasicFrameCtrl(stmt.Body.Body, state0.IsCurFramePartial(), null);
      state.AddNewFrame(framectrl);

      var matchStmt = RawCodeList[0][0] as MatchStmt;

      var idx = GetNthCaseIdx(RawCodeList);
      if (matchStmt != null)
        foreach(var tmp in matchStmt.Cases[idx].CasePatterns) {
          state.AddDafnyVar(tmp.Var.Name, new ProofState.VariableData { Variable = tmp.Var, Type = tmp.Var.Type });
        }
      //with this flag set to true, dafny will check the case branch before evaluates any tacny code
      state.NeedVerify = true;
      yield return state;
     
    }

    public override IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
      Contract.Requires(statement != null);
      Contract.Requires(statement is TacticCasesBlockStmt);
      var partial = false || state0.IsCurFramePartial();

      var state = state0.Copy();

      var stmt = statement as TacticCasesBlockStmt;
      NameSegment caseVar;

      //get guards
      Debug.Assert(stmt != null, "stmt != null");
      var guard = stmt.Guard as ParensExpression;

      if(guard == null)
        caseVar = stmt.Guard as NameSegment;
      else
        caseVar = guard.E as NameSegment;

      //TODO: need to check the datatype pf caseGuard, 
      // also need to consider the case that caseVar is a tac var
      var srcVar = state.GetTVarValue(caseVar) as NameSegment;
      //var srcVarData = state.GetDafnyVar(srcVar.Name);
      if (srcVar != null)
      {
        var datatype = state.GetDafnyVarType(srcVar.Name).AsDatatype;


        //generate a test program to check which cases need to apply tacny
        bool[] ctorFlags;
        InitCtorFlags(datatype, out ctorFlags);

        List<Func<int, List<Statement>>> fList = new List<Func<int, List<Statement>>>();

        int i;
        for(i = 0; i < datatype.Ctors.Count; i++) {
          fList.Add(GenerateAssumeFalseStmtAsStmtList);
        }

        //var matchStmt = GenerateMatchStmt(state.TacticApplication.Tok.line, srcVar.Copy(), datatype, fList);
        var matchStmt = GenerateMatchStmt(Interpreter.TacticCodeTokLine, srcVar.Copy(), datatype, fList);

        //use a dummystmt to creat a frame for match, note that this stmts is never be evaluated
        var dummystmt = new List<Statement>();
        for(i = 0; i < datatype.Ctors.Count; i++) {
          dummystmt.Add(stmt);
        }

        var matchCtrl = this;

        matchCtrl.InitBasicFrameCtrl(dummystmt, partial, null);
        state.AddNewFrame(matchCtrl);

        //add raw[0]
        state.AddStatement(matchStmt);

        //push a frame for the first case
        //TODO: add case variable to frame, so that variable () can refer to it
        var caseCtrl = new DefaultTacticFrameCtrl();
        caseCtrl.InitBasicFrameCtrl(stmt.Body.Body, partial, null);
        state.AddNewFrame(caseCtrl);

        foreach(var tmp in matchStmt.Cases[0].CasePatterns) {
          state.AddDafnyVar(tmp.Var.Name, new ProofState.VariableData { Variable = tmp.Var, Type = tmp.Var.Type });
        }
      }
      //with this flag set to true, dafny will check the case brnach before evaluates any tacny code
      state.NeedVerify = true;
      yield return state;
    }
    /*
    private IEnumerable<ProofState> StmtHandler(List<Statement> stmts, ProofState state){
      IEnumerable<ProofState> enumerable = null;
      ProofState ret = state;

      foreach (var stmt in stmts){
        if (stmt is TacticVarDeclStmt){
          enumerable = Interpreter.RegisterVariable(stmt as TacticVarDeclStmt, ret);
          var e = enumerable.GetEnumerator();
          e.MoveNext();
          ret = e.Current;
        }
        else if (stmt is PredicateStmt){
          enumerable = Interpreter.EvalPredicateStmt((PredicateStmt) stmt, ret);
          var e = enumerable.GetEnumerator();
          e.MoveNext();
          ret = e.Current;
        }
      }

      foreach(var item in enumerable)
        yield return item.Copy();
    }
    */

    private static void InitCtorFlags(DatatypeDecl datatype, out bool[] flags, bool value = false) {
      flags = new bool[datatype.Ctors.Count];
      for(int i = 0; i < flags.Length; i++) {
        flags[i] = value;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <param name="ns"></param>
    /// <param name="datatype"></param>
    /// <param name="fL"></param>a function list which contains a function to generate stetment list with given line number
    /// <returns></returns>
    private MatchStmt GenerateMatchStmt (int line, NameSegment ns, DatatypeDecl datatype, List<Func<int, List<Statement>>> fL) {
      Contract.Requires(ns != null);
      Contract.Requires(datatype != null);
      Contract.Ensures(Contract.Result<MatchStmt>() != null);
      List<MatchCaseStmt> cases = new List<MatchCaseStmt>();
      int index = Interpreter.TacticCodeTokLine;//line + 1;


      for (int j = 0; j < datatype.Ctors.Count; j++){
        var dc = datatype.Ctors[j];
        Func<int, List<Statement>> f = _=> new List<Statement>();
        if (j < fL.Count) f = fL[j];

        MatchCaseStmt mcs = GenerateMatchCaseStmt(index, dc, f);

        cases.Add(mcs);
        line += mcs.Body.Count + 1;
      }

      return new MatchStmt(new Token(index, 0) { val = "match" },
        new Token(index, 0) { val = "=>"}, 
        ns, cases, false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <param name="dtc"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    private MatchCaseStmt GenerateMatchCaseStmt(int line, DatatypeCtor dtc,  Func<int,List<Statement>> f) {
      Contract.Requires(dtc != null);
      List<CasePattern> casePatterns = new List<CasePattern>();
      dtc = new DatatypeCtor(dtc.tok, dtc.Name, dtc.Formals, dtc.Attributes);

      foreach(var formal in dtc.Formals)
      {
        var cp = GenerateCasePattern(line, formal);
        casePatterns.Add(cp);
      }

      //List<Statement> body = new List<Statement>();
      //body.Add(GenerateAssumeFalseStmt(line));
         var mcs = new MatchCaseStmt(new Token(line, 0) { val = "cases" },
           dtc.CompileName, casePatterns, f(line));
      return mcs;
    }

    private List<Statement> GenerateAssumeFalseStmtAsStmtList(int line){
      var l = new List<Statement> {GenerateAssumeFalseStmt(line)};
      return l;
    } 

    private AssumeStmt GenerateAssumeFalseStmt(int line){
      return new AssumeStmt(new Token(line, 0){val = "assume"},
        new Token(line, 0){val = ";"},
        new Microsoft.Dafny.LiteralExpr(new Token(line, 0) { val = "false" }, false), 
        null);
      
    }

    private CasePattern GenerateCasePattern(int line, Formal formal) {
      Contract.Requires(formal != null);
/*      var name = PopCaseName();
      if (name == null) name = formal.Name; 
 */
      formal = new Formal(formal.tok, formal.Name, formal.Type, formal.InParam, formal.IsGhost);
      CasePattern cp = new CasePattern(new Token(line, 0) { val = formal.Name },
        new BoundVar(new Token(line, 0) { val = formal.Name }, formal.Name, new InferredTypeProxy()));
      return cp;
    }

  }
}