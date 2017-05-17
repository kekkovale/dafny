using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using Microsoft.Boogie;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny
{
  public class TacnyDriver
  {
    public static int TacticCodeTokLine = -1;
    public static bool IfEvalTac { get; set; } = true;

    private static TacnyDriver _driver;
    private static ErrorReporterDelegate _errorReporterDelegate;
    private static Dictionary<Statement, List<Statement>> _resultList;

    private Stack<Dictionary<IVariable, Type>> _frame;
    private readonly ProofState _state;

    public static Stopwatch Timer;

    public static void ResetTacticResultList()
    {
      if (Timer == null)
        Timer = new Stopwatch();

      if (_resultList == null)
        _resultList = new Dictionary<Statement, List<Statement>>();
      else
        _resultList.Clear();

      EAtomic.EAtomic.InitEAtomicSigList();
    }

    public static Dictionary<IToken, List<Statement>> GetTacticResultList()
    {
      Dictionary<IToken, List<Statement>> bufferList = new Dictionary<IToken, List<Statement>>();
      foreach (var e in _resultList) {
        bufferList.Add(e.Key.Tok, e.Value);
      }
      return bufferList;
    }

    private TacnyDriver(Program program)
    {
      Contract.Requires(Tcce.NonNull(program));
      // initialize state
      _state = new ProofState(program);
      _frame = new Stack<Dictionary<IVariable, Type>>();
    }


    /// <param name="r"></param>
    /// <returns></returns>
    public static MemberDecl ApplyTacticInMethod(Program program, MemberDecl target, ErrorReporterDelegate erd,
      Resolver r = null)
    {
      Contract.Requires(program != null);
      Contract.Requires(target != null);
      Stopwatch watch = new Stopwatch();
      watch.Start();
      Timer.Restart();
      _driver = new TacnyDriver(program);
      _errorReporterDelegate = erd;
      Type.BackupScopes();
      var result = _driver.InterpretAndUnfoldTactic(target, r);
      Type.RestoreScopes();
      var p = new Printer(Console.Out);
      p.PrintMembers(new List<MemberDecl>() {result}, 0, "");

      watch.Stop();
      Console.WriteLine("Time Used: " + watch.Elapsed.TotalSeconds);
      _errorReporterDelegate = null;
      return result;
    }


    private MemberDecl InterpretAndUnfoldTactic(MemberDecl target, Resolver r)
    {
      Contract.Requires(Tcce.NonNull(target));
      // initialize new stack for variables
      _frame = new Stack<Dictionary<IVariable, Type>>();

      var method = target as Method;
      if (method != null) {
        _state.SetTopLevelClass(method.EnclosingClass?.Name);
        _state.TargetMethod = target;
        var dict = method.Ins.Concat(method.Outs)
          .ToDictionary<IVariable, IVariable, Type>(item => item, item => item.Type);
        _frame.Push(dict);

        var preRes = _resultList.Keys.Copy();

        InterpertBlockStmt(method.Body);
        dict = _frame.Pop();
        // sanity check
        Contract.Assert(_frame.Count == 0);

        var newRets = _resultList.Where(kvp => !preRes.Contains(kvp.Key)).ToDictionary(i => i.Key, i => i.Value);
        Contract.Assert(newRets.Count != 0);
        var body = Util.InsertCode(_state, newRets);
        method.Body.Body.Clear();
        if (body != null)
          method.Body.Body.AddRange(body.Body);


        // use the original resolver of the resoved program, as it contains all the necessary type info
        method.CallsTactic = 0; 
        // set the current class in the resolver, so that it can refer to the memberdecl correctly
        r.SetCurClass(method.EnclosingClass as ClassDecl);
        //asssume the defualt module is the current module, this needs to be improved.
        r.ResolveMethodBody(method, _state.GetDafnyProgram().DefaultModuleDef.Name);
        //Console.WriteLine("Errors: " + _program.reporter.Count(ErrorLevel.Error));

      }
      return method;
    }

    // Find tactic application and resolve it
    private void InterpertBlockStmt(BlockStmt body)
    {
      Contract.Requires(Tcce.NonNull(body));

      // BaseSearchStrategy.ResetProofList();
      _frame.Push(new Dictionary<IVariable, Type>());
      foreach (var stmt in body.Body) {
        if (stmt is VarDeclStmt) {
          var vds = stmt as VarDeclStmt;
          // register local variable declarations
          foreach (var local in vds.Locals) {
            try {
              _frame.Peek().Add(local, local.Type);
            } catch (Exception e) {
              //TODO: some error handling when target is not resolved
              Console.Out.WriteLine(e.Message);
            }
          }
        } else if (stmt is IfStmt) {
          var ifStmt = stmt as IfStmt;
          InterpretIfStmt(ifStmt);

        } else if (stmt is WhileStmt) {
          var whileStmt = stmt as WhileStmt;
          InterpretWhileStmt(whileStmt);
        } else if (stmt is UpdateStmt) {
          if (_state.IsTacticCall(stmt as UpdateStmt)) {
            UndfoldTacticCall(stmt);
          }
        } else if (stmt is InlineTacticBlockStmt) {
          UndfoldTacticCall(stmt);
        } else if (stmt is BlockStmt) {
          InterpertBlockStmt((stmt as BlockStmt));
        }
      }
      _frame.Pop();
    }

    private void UndfoldTacticCall(Statement stmt)
    {
      var list = StackToDict(_frame);
      // this is a top level tactic call
      ProofState result = null;
      if (IfEvalTac) {
        result = TacnyInterpreter.EvalTopLevelTactic(_state, list, stmt, _errorReporterDelegate);
      }
      _resultList.Add(stmt.Copy(), result != null ? result.GetGeneratedCode().Copy() : new List<Statement>());
    }

    private void InterpretWhileStmt(WhileStmt stmt)
    {
      if (stmt.TInvariants != null && stmt.TInvariants.Count > 0) {
        foreach (var tinv in stmt.TInvariants) {
          if (tinv is UpdateStmt) {
            var list = StackToDict(_frame);


            // this is a top level tactic call
            ProofState result = null;
            if (IfEvalTac) {
              result = TacnyInterpreter.EvalTopLevelTactic(_state, list, tinv as UpdateStmt, _errorReporterDelegate);
            }
            if (result != null)
              _resultList.Add(tinv as UpdateStmt, result.GetGeneratedCode().Copy());
          }
        }
      }
      InterpertBlockStmt(stmt.Body);
    }

    private void InterpretIfStmt(IfStmt ifStmt)
    {
      Contract.Requires(Tcce.NonNull(ifStmt));
      //throw new NotImplementedException();

      InterpertBlockStmt(ifStmt.Thn);
      if (ifStmt.Els == null)
        return;
      var els = ifStmt.Els as BlockStmt;
      if (els != null) {
        InterpertBlockStmt(els);
      } else if (ifStmt.Els is IfStmt) {
        InterpretIfStmt((IfStmt) ifStmt.Els);
      }
    }

    private static Dictionary<IVariable, Type> StackToDict(Stack<Dictionary<IVariable, Type>> stack)
    {
      Contract.Requires(stack != null);
      Contract.Ensures(Contract.Result<Dictionary<IVariable, Type>>() != null);
      var result = new Dictionary<IVariable, Type>();
      foreach (var dict in stack) {
        dict.ToList().ForEach(x => result.Add(x.Key, x.Value));
      }
      return result;
    }

  }
}
