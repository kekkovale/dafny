using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using Dfy = Microsoft.Dafny;
using Microsoft.Dafny.Tacny.Language;

namespace Microsoft.Dafny.Tacny{
  public class ProofState{
    // Static State
    public readonly Dictionary<string, DatatypeDecl> Datatypes;
    public TopLevelClassDeclaration ActiveClass;
    private readonly List<TopLevelClassDeclaration> _topLevelClasses;
    private readonly Program _original;

    public ErrHandler err;
    // Dynamic State
    public MemberDecl TargetMethod;
    public ErrorReporter Reporter;

    //not all the eval step requires to be verified, e.g. var decl
    public bool NeedVerify { set; get; } = false;

    public UpdateStmt TopLevelTacApp;
/*
    public ITactic ActiveTactic {
      get {
        Contract.Assert(_scope != null);
        Contract.Assert(_scope.Count > 0);
        return _scope.Peek().ActiveTactic;
      }
    }
 */
    private Stack<Frame> _scope;

    public ProofState(Program program, ErrorReporter reporter){
      Contract.Requires(program != null);
      // get a new program instance
      Datatypes = new Dictionary<string, DatatypeDecl>();
      _topLevelClasses = new List<TopLevelClassDeclaration>();
      Reporter = reporter;

      var files = new List<DafnyFile>();
      files.Add(new DafnyFile(program.FullName));
      //note the differences between this ParseCheck and the one at the top level. This function only parses but the other one resolves.
      //use the deafult error reportor, as the one from program include too much extra object/information for deep copy
      var err = Main.Parse(files, program.Name, new ConsoleErrorReporter(), out _original);
      if (err != null)
        reporter.Error(MessageSource.Tacny, program.DefaultModuleDef.tok, $"Error parsing a fresh Tacny program: {err}");

      // fill state
      FillStaticState(program);
    }

    /// <summary>
    /// Initialize a new tactic state
    /// </summary>
    /// <param name="tacAps">Tactic application</param>
    /// <param name="variables">Dafny variables</param>
    public void InitState(UpdateStmt tacAps, Dictionary<IVariable, Dfy.Type> variables){
      // clear the scope  
      _scope = new Stack<Frame>();

      var tactic = GetTactic(tacAps) as Tactic;

      var aps = ((ExprRhs) tacAps.Rhss[0]).Expr as ApplySuffix;
      if (aps.Args.Count != tactic.Ins.Count)
        Reporter.Error(MessageSource.Tacny, tacAps.Tok,
          $"Wrong number of method arguments (got {aps.Args.Count}, expected {tactic.Ins.Count})");

      var frame = new Frame(tactic, tacAps.Rhss[0].Attributes, Reporter);

      foreach (var item in variables){
        if (!frame.ContainDafnyVar(item.Key.Name))
          frame.AddDafnyVar(item.Key.Name, new VariableData{Variable = item.Key, Type = item.Value});
        else
          throw new ArgumentException($"Dafny variable {item.Key.Name} is already declared in the current context");
      }

      for (int index = 0; index < aps.Args.Count; index++){
        var arg = aps.Args[index];
        frame.AddTVar(tactic.Ins[index].Name, arg);
      }

      frame.tokenTracer = new TokenTracer(tacAps.Tok);

      _scope.Push(frame);
      TopLevelTacApp = tacAps.Copy();
    }

    // Permanent state information
    public Dictionary<string, ITactic> Tactics => ActiveClass.Tactics;
    public Dictionary<string, MemberDecl> Members => ActiveClass.Members;


    public Program GetDafnyProgram(){
      //Contract.Requires(_original != null, "_original");
      Contract.Ensures(Contract.Result<Program>() != null);
      var copy = _original.Copy();
      return copy;
    }


    /// <summary>
    ///   Set active the enclosing TopLevelClass
    /// </summary>
    /// <param name="name"></param>
    public void SetTopLevelClass(string name){
      ActiveClass = _topLevelClasses.FirstOrDefault(x => x.Name == name);
    }

    /// <summary>
    ///   Fill permanent state information, which will be common across all tactics
    /// </summary>
    /// <param name="program">fresh Dafny program</param>
    private void FillStaticState(Program program){
      Contract.Requires<ArgumentNullException>(program != null);


      foreach (var item in program.DefaultModuleDef.TopLevelDecls){
        var curDecl = item as ClassDecl;
        if (curDecl != null){
          var temp = new TopLevelClassDeclaration(curDecl.Name);

          foreach (var member in curDecl.Members){
            var tac = member as ITactic;
            if (tac != null)
              temp.Tactics.Add(tac.Name, tac);
            else{
              temp.Members.Add(member.Name, member);
            }
          }
          _topLevelClasses.Add(temp);
        }
        else{
          var dd = item as DatatypeDecl;
          if (dd != null)
            Datatypes.Add(dd.Name, dd);
        }
      }
    }


    public void AddNewFrame(TacticFrameCtrl ctrl){
      var parent = _scope.Peek();
      _scope.Push(new Frame(parent, ctrl));
    }
    // note that this function would only be called when either a frame is proved or isEvaluated.
    public void MarkCurFrameAsTerminated(bool curFrameProved, bool backtracked){

      //assemble code in the top frame. the stata that code is null after this call, indicates
      // the current branches has been backtrackee.
      // 
      bool ifbacktracked, ifbacktrackedInRecurCall = false;
      _scope.Peek().FrameCtrl.MarkAsEvaluated(curFrameProved, out ifbacktracked);

      var code = _scope.Peek().FrameCtrl.GetFinalCode();
      var trace = _scope.Peek().tokenTracer;

      // add the assembled code to the parent frame
      if (code != null && _scope.Peek().Parent != null){
        _scope.Peek().Parent.FrameCtrl.AddGeneratedCode(code);
        _scope.Peek().Parent.tokenTracer.AddSubTrace(trace);
        _scope.Pop();
        if (_scope.Peek().FrameCtrl.EvalTerminated(curFrameProved, this) || IsEvaluated())
          MarkCurFrameAsTerminated(curFrameProved, ifbacktrackedInRecurCall);
      }
      backtracked = ifbacktracked || ifbacktrackedInRecurCall;
    }

    public IEnumerable<ProofState> EvalStep(){
      return _scope.Peek().FrameCtrl.EvalStep(this);
    }

    // various getters

    #region GETTERS

    internal TokenTracer GetTokenTracer(Frame f, TokenTracer tracer0) {
      var tracer = f.tokenTracer.Copy();
      tracer.AddSubTrace(tracer0);

      if (f.Parent == null)
        return tracer;
      else
        return GetTokenTracer(f.Parent, tracer);
    }
    public TokenTracer GetTokenTracer() {
      var top = _scope.Peek();
      if (top.Parent == null)
        return top.tokenTracer;
      else
       return GetTokenTracer(top.Parent, top.tokenTracer);
    }

    public TokenTracer TopTokenTracer() {
      var top = _scope.Peek();
      return top.tokenTracer;
    }

    public Statement GetStmt(){
      return _scope.Peek().FrameCtrl.GetStmt();
    }

    public Strategy GetSearchStrategy(){
      return _scope.Peek().FrameCtrl.SearchStrategy;
    }

    public bool IsCurFramePartial(){
      return _scope.Peek().FrameCtrl.IsPartial;
    }

    public List<int> GetBackTrackCount(){
      var frame = _scope.Peek();
      var backtrack = new List<int>();
      backtrack.Add(frame.FrameCtrl.Backtrack);

      while (frame.Parent != null){
        frame = frame.Parent;
        backtrack.Add(frame.FrameCtrl.Backtrack);
      }

      return backtrack;
    }

    public int GetOrignalTopBacktrack(){
      return _scope.Peek().FrameCtrl.OriginalBK;
    }

    public void SetBackTrackCount(List<int> cnt){
      var cur = GetBackTrackCount();
      //restore from the root
      cur.Reverse();
      List<int> tmp = cnt.Copy();
      tmp.Reverse();

      for (int j = 0; j < cur.Count; j++){
        int count;
        if (j >= tmp.Count)
          count = cur[j];
        else
          count = tmp[j];
        cur[j] = count;
      }

      cur.Reverse();

      var frame = _scope.Peek();
      frame.FrameCtrl.Backtrack = cur[0];
      cur.RemoveAt(0);

      while(frame.Parent != null) {
        frame = frame.Parent;
        frame.FrameCtrl.Backtrack = cur[0];
        cur.RemoveAt(0);
      }
    }
    /// <summary>
    /// a proof state is verified if there is only one frame in the stack and _genratedCode is not null (raw code are assembled)
    /// </summary>
    /// <returns></returns>
    public bool IsTerminated(){
      return _scope.Count == 1 && _scope.Peek().FrameCtrl.GetFinalCode() != null;
    }

    /// <summary>
    /// Check if the current frame is fully interpreted by tracking counts of stmts
    /// </summary>
    /// <returns></returns>
    public bool IsEvaluated(){
      return _scope.Peek().FrameCtrl.IsEvaluated;
    }


    public List<Statement> GetGeneratedCode(){
      // Contract.Ensures(Contract.Result<List<Statement>>() != null);
      return _scope.Peek().GetGeneratedCode();
    }

    /// <summary>
    ///   Check if Dafny key exists in the current context
    /// </summary>
    /// <param name="key">Variable name</param>
    /// <returns>bool</returns>
    public bool ContainDafnyVar(string key){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(key));
      return _scope.Peek().ContainDafnyVar(key);
    }


    /// <summary>
    ///   Check if Dafny key exists in the current context
    /// </summary>
    /// <param name="key">Variable</param>
    /// <returns>bool</returns>

    public bool ContainDafnyVar(NameSegment key){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(key));
      return ContainDafnyVar(key.Name);
    }

    /// <summary>
    ///   Return Dafny key
    /// </summary>
    /// <param name="key">Variable name</param>
    /// <returns>bool</returns>
    /// <exception cref="KeyNotFoundException">Variable does not exist in the current context</exception>
    public IVariable GetDafnyVar(string key){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(key));
      Contract.Ensures(Contract.Result<IVariable>() != null);
      if (ContainDafnyVar(key))
        return _scope.Peek().GetDafnyVariableData(key).Variable;
      throw new KeyNotFoundException($"Dafny variable {key} does not exist in the current context");
    }

    /// <summary>
    ///   Return Dafny key
    /// </summary>
    /// <param name="key">Variable name</param>
    /// <returns>bool</returns>
    /// <exception cref="KeyNotFoundException">Variable does not exist in the current context</exception>
    public IVariable GetDafnyVar(NameSegment key){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(key));
      Contract.Ensures(Contract.Result<IVariable>() != null);
      return GetDafnyVar(key.Name);
    }

    /// <summary>
    /// get a dictionary containing all the dafny variable in current scope, including all the frame. If the variable will be ignore, if it confilts with some other top frames
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, VariableData> GetAllDafnyVars(){
      return _scope.Peek().GetAllDafnyVars(new Dictionary<string, VariableData>());
    }

    /// <summary>
    ///   Return the type of the key
    /// </summary>
    /// <param name="variable">key</param>
    /// <returns>null if type is not known</returns>
    /// <exception cref="KeyNotFoundException">Variable does not exist in the current context</exception>
    public Dfy.Type GetDafnyVarType(IVariable variable){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(variable));
      Contract.Ensures(Contract.Result<Dfy.Type>() != null);
      return GetDafnyVarType(variable.Name);
    }

    /// <summary>
    ///   Return the type of the key
    /// </summary>
    /// <param name="key">name of the key</param>
    /// <returns>null if type is not known</returns>
    /// <exception cref="KeyNotFoundException">Variable does not exist in the current context</exception>
    public Dfy.Type GetDafnyVarType(string key){
      Contract.Requires<ArgumentNullException>(tcce.NonNull(key));
      Contract.Ensures(Contract.Result<Dfy.Type>() != null);
      if (ContainDafnyVar(key))
        return GetDafnyVar(key).Type;
      throw new KeyNotFoundException($"Dafny variable {key} does not exist in the current context");
    }

    /// <summary>
    /// Get the value of local variable
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object GetTVarValue(NameSegment key){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      Contract.Ensures(Contract.Result<object>() != null);
      return GetTVarValue(key.Name);
    }

    /// <summary>
    /// Get the value of local variable
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object GetTVarValue(string key){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      Contract.Ensures(Contract.Result<object>() != null);
      return _scope.Peek().GetTValData(key);
    }

    public bool ContainTVal(NameSegment key){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      return ContainTVal(key.Name);
    }

    public Dictionary<string, object> GetAllTVars() {
      return _scope.Peek().GetAllTVars(new Dictionary<string, object>());
    }

    /// <summary>
    /// Check if Tacny variable has been declared
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainTVal(string key){
      Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key), "key");
      if (_scope == null || _scope.Count == 0) return false;
      return _scope.Peek().ContainTVars(key);
    }

    private ITactic GetTactic(string name){
      Contract.Requires<ArgumentNullException>(name != null);
      Contract.Requires<ArgumentNullException>(Tactics.ContainsKey(name), "Tactic does not exist in the current context");
      Contract.Ensures(Contract.Result<ITactic>() != null);

      return Tactics[name];
    }

    /// <summary>
    /// Get called tactic
    /// </summary>
    /// <param name="us"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"> </exception>
    /// /// <exception cref="ArgumentException"> Provided UpdateStmt is not a tactic application</exception>
    public ITactic GetTactic(UpdateStmt us){
      Contract.Requires(us != null);
      Contract.Requires<ArgumentException>(IsTacticCall(us));
      Contract.Ensures(Contract.Result<ITactic>() != null);

      var name = Util.GetSignature(us);
      if(ContainTVal(name)) {
        var nameSegment = GetTVarValue(name) as NameSegment;
        if(nameSegment != null)
          name = nameSegment.Name;
      }

      return GetTactic(name);
    }

    /// <summary>
    /// Get called tactic
    /// </summary>
    /// <param name="aps"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"> </exception>
    /// <exception cref="ArgumentException"> Provided ApplySuffix is not a tactic application</exception>
    public ITactic GetTactic(ApplySuffix aps){
      Contract.Requires(aps != null);
      Contract.Requires(IsTacticCall(aps));
      Contract.Ensures(Contract.Result<ITactic>() != null);
      var name = Util.GetSignature(aps);
      if(ContainTVal(name)) {
        var nameSegment = GetTVarValue(name) as NameSegment;
        if(nameSegment != null)
          name = nameSegment.Name;
      }
      return GetTactic(name);
    }

    #endregion GETTERS

    // helper methods

    #region HELPERS

    /// <summary>
    ///   Check if an UpdateStmt is a tactic call
    /// </summary>
    /// <param name="us"></param>
    /// <returns></returns>
    [Pure]
    public bool IsTacticCall(UpdateStmt us){
      Contract.Requires(us != null);
      var name = Util.GetSignature(us);
      if (ContainTVal(name)){
        var nameSegment = GetTVarValue(name) as NameSegment;
        if (nameSegment != null) name = nameSegment.Name;
      }
      return IsTacticCall(name);
    }

    /// <summary>
    ///   Check if an ApplySuffix is a tactic call
    /// </summary>
    /// <param name="aps"></param>
    /// <returns></returns>
    [Pure]
    public bool IsTacticCall(ApplySuffix aps){
      Contract.Requires(aps != null);
      return IsTacticCall(Util.GetSignature(aps));
    }

    private bool IsTacticCall(string name){
      Contract.Requires(tcce.NonNull(name));
      if (name == null) return false;
      return Tactics.ContainsKey(name);
    }

    #endregion HELPERS

    /// <summary>
    /// Check in an updateStmt is local assignment
    /// </summary>
    /// <param name="us"></param>
    /// <returns></returns>
    [Pure]
    public bool IsLocalAssignment(UpdateStmt us){
      if (us.Lhss.Count == 0)
        return false;
      foreach (var lhs in us.Lhss){
        if (!(lhs is NameSegment))
          return false;
        if (!_scope.Peek().ContainTVars((lhs as NameSegment).Name))
          return false;
      }

      return true;
    }

    [Pure]
    public bool IsArgumentApplication(UpdateStmt us){
      Contract.Requires<ArgumentNullException>(us != null, "us");
      var ns = Util.GetNameSegment(us);
      return _scope.Peek().ContainTVars(ns.Name);
    }

    /// <summary>
    /// Add a varialbe to the top level frame
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddTacnyVar(IVariable key, object value){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      Contract.Requires<ArgumentException>(!ContainTVal(key.Name));
      AddTacnyVar(key.Name, value);
    }

    /// <summary>
    /// Add a varialbe to the top level frame
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddTacnyVar(string key, object value){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      Contract.Requires<ArgumentException>(!ContainTVal(key));
      _scope.Peek().AddTVar(key, value);
    }

    /// <summary>
    /// Update a local variable
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void UpdateTacnyVar(IVariable key, object value){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      UpdateTacnyVar(key.Name, value);
    }

    /// <summary>
    /// Update a local variable
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void UpdateTacnyVar(string key, object value){
      Contract.Requires<ArgumentNullException>(key != null, "key");
      _scope.Peek().UpdateLocalTVar(key, value);
    }

    /// <summary>
    /// add a dafny variable to the top frame
    /// </summary>
    /// <param name="name"></param>
    /// <param name="var"></param>
    public void AddDafnyVar(string name, VariableData var){
      _scope.Peek().AddDafnyVar(name, var);
    }

    /// <summary>
    /// Add new dafny stmt to the top frame
    /// </summary>
    /// <param name="stmt"></param>
    public void AddStatement(Statement stmt){
      Contract.Requires<ArgumentNullException>(stmt != null, "stmt");
      _scope.Peek().FrameCtrl.AddGeneratedCode(stmt);
    }

    /// <summary>
    /// Add new dafny stmt to the top frame
    /// </summary>
    /// <param name="stmtList"></param>
    public void AddStatementRange(List<Statement> stmtList){
      Contract.Requires<ArgumentNullException>(tcce.NonNullElements(stmtList));
      _scope.Peek().FrameCtrl.AddGeneratedCode(stmtList);
    }

    /// <summary>
    /// Return the latest unevalauted statement from the top frame
    /// </summary>
    /// <param name="partial"></param>
    /// <returns></returns>


    public class TopLevelClassDeclaration{
      public readonly Dictionary<string, MemberDecl> Members;
      public readonly string Name;
      public readonly Dictionary<string, ITactic> Tactics;

      public TopLevelClassDeclaration(string name){
        Contract.Requires(name != null);
        Tactics = new Dictionary<string, ITactic>();
        Members = new Dictionary<string, MemberDecl>();
        Name = name;
      }
    }

    internal class Frame{
      public readonly Frame Parent;
      private readonly Dictionary<string, object> _declaredVariables; // tacny variables
      private readonly Dictionary<string, VariableData> _DafnyVariables; // dafny variables
      public TacticFrameCtrl FrameCtrl;
      public TokenTracer tokenTracer;
      private readonly ErrorReporter _reporter;

      /// <summary>
      /// Initialize the top level frame
      /// </summary>
      /// <param name="tactic"></param>
      /// <param name="reporter"></param>
      public Frame(ITactic tactic, Attributes attr, ErrorReporter reporter){
        Contract.Requires<ArgumentNullException>(tactic != null, "tactic");
        Parent = null;
        var o = tactic as Tactic;
        if (o == null){
          throw new NotSupportedException("tactic functions are not yet supported");
        }

        FrameCtrl = new DefaultTacticFrameCtrl();
        FrameCtrl.InitBasicFrameCtrl(o.Body.Body, false, attr, o);

        _reporter = reporter;
        _declaredVariables = new Dictionary<string, object>();
        _DafnyVariables = new Dictionary<string, VariableData>();

      }

      public Frame(Frame parent, TacticFrameCtrl ctrl){
        Contract.Requires<ArgumentNullException>(parent != null);
        // carry over the tactic info
        _declaredVariables = new Dictionary<string, object>();
        _DafnyVariables = new Dictionary<string, VariableData>();
        tokenTracer = parent.tokenTracer.GenSubTrace();
        Parent = parent;
        _reporter = parent._reporter;
        FrameCtrl = ctrl;
      }

      [Pure]
      internal VariableData GetLocalDafnyVar(string name){
        //Contract.Requires(_DafnyVariables.ContainsKey(name));
        return _DafnyVariables[name];
      }

      internal void AddDafnyVar(string name, VariableData var){
        Contract.Requires<ArgumentNullException>(name != null, "key");
        if (_DafnyVariables.All(v => v.Key != name)){
          _DafnyVariables.Add(name, var);
        }
        else{
          throw new ArgumentException($"dafny var {name} is already declared in the scope");
        }
      }

      internal bool ContainDafnyVar(string name){
        Contract.Requires<ArgumentNullException>(name != null, "name");
        // base case
        if (Parent == null)
          return _DafnyVariables.Any(kvp => kvp.Key == name);
        return _DafnyVariables.Any(kvp => kvp.Key == name) || Parent.ContainDafnyVar(name);
      }


      internal VariableData GetDafnyVariableData(string name){
//     Contract.Requires(ContainDafnyVars(name));
        if (_DafnyVariables.ContainsKey(name))
          return _DafnyVariables[name];
        else{
          return Parent.GetDafnyVariableData(name);
        }
      }

      internal Dictionary<string, VariableData> GetAllDafnyVars(Dictionary<string, VariableData> toDict){
        _DafnyVariables.Where(x => !toDict.ContainsKey(x.Key)).ToList().ForEach(x => toDict.Add(x.Key, x.Value));
        if (Parent == null)
          return toDict;
        else{
          return Parent.GetAllDafnyVars(toDict);
        }
      }


      internal bool ContainTVars(string name){
        Contract.Requires<ArgumentNullException>(name != null, "name");
        // base case
        if (Parent == null)
          return _declaredVariables.Any(kvp => kvp.Key == name);
        return _declaredVariables.Any(kvp => kvp.Key == name) || Parent.ContainTVars(name);
      }

      internal void AddTVar(string variable, object value){
        Contract.Requires<ArgumentNullException>(variable != null, "key");
        if (_declaredVariables.All(v => v.Key != variable)){
          _declaredVariables.Add(variable, value);
        }
        else{
          throw new ArgumentException($"tacny var {variable} is already declared in the scope");
        }
      }

      internal void UpdateLocalTVar(IVariable key, object value){
        Contract.Requires<ArgumentNullException>(key != null, "key");
        Contract.Requires<ArgumentException>(ContainTVars(key.Name));
        //, $"{key} is not declared in the current scope".ToString());
        UpdateLocalTVar(key.Name, value);
      }

      internal void UpdateLocalTVar(string key, object value){
        Contract.Requires<ArgumentNullException>(key != null, "key");
        //Contract.Requires<ArgumentException>(_declaredVariables.ContainsKey(key));
        if (_declaredVariables.ContainsKey(key))
          _declaredVariables[key] = value;
        else{
          Parent.UpdateLocalTVar(key, value);
        }
      }

      internal object GetTValData(string name){
        Contract.Requires<ArgumentNullException>(name != null, "key");
        //Contract.Requires<ArgumentException>(ContainTVars(key));
        Contract.Ensures(Contract.Result<object>() != null);
        if (_declaredVariables.ContainsKey(name))
          return _declaredVariables[name];
        else{
          return Parent.GetTValData(name);
        }
      }
      internal Dictionary<string, object> GetAllTVars(Dictionary<string, object> toDict) {
        _declaredVariables.Where(x => !toDict.ContainsKey(x.Key)).ToList().ForEach(x => toDict.Add(x.Key, x.Value));
        if (Parent == null)
          return toDict;
        else {
          return Parent.GetAllTVars(toDict);
        }
      }
      internal List<Statement> GetGeneratedCode(){
        var code = GetGeneratedCode0();
        return code;
      }

      internal List<Statement> GetGeneratedCode0(List<Statement> stmts = null){
        Contract.Ensures(Contract.Result<List<Statement>>() != null);
        List<Statement> code = FrameCtrl.GetFinalCode();
        if (code != null){
// terminated, so just use the assembly code 
        }
        else if (stmts != null){
          // for the case when code are addded by child, and the child has assembly the code for parent
          code = stmts;
        }
        else{
          // new code from child and not terminated, just assmeble the current one 
          code = FrameCtrl.AssembleStmts(FrameCtrl.GetRawCode());
        }

        if (Parent == null)
          return code.Copy();
        else{
          // parent is always not yet terminated, so assmebly code for it
          var parRawCode = Parent.FrameCtrl.GetRawCode().Copy();
          parRawCode.Add(code);
          var parCode = Parent.FrameCtrl.AssembleStmts(parRawCode);
          return Parent.GetGeneratedCode0(parCode);
        }
      }
    }


    public class VariableData{
      private Dfy.Type _type;

      private IVariable _variable;

      public IVariable Variable{
        get { return _variable; }
        set{
          Contract.Assume(_variable == null); // key value should be only set once
          Contract.Assert(tcce.NonNull(value));
          _variable = value;
        }
      }

      public Dfy.Type Type{
        get { return _type; }
        set{
          Contract.Assume(_type == null);
          _type = value;
        }
      }
    }
  }
}