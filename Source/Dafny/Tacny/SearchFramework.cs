using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Boogie;


namespace Microsoft.Dafny.Tacny {
  
  [ContractClass(typeof(BaseSearchContract))]
  public interface ISearch {
    IEnumerable<ProofState> Search(ProofState state, ErrorReporterDelegate er);
  }


  public enum Strategy {
    Undefined = 0,
    Bfs,
    Dfs
  }

  public enum VerifyResult {
    Unresolved, // failed to resolve
    Verified,
    Failed, // resoved but cannot be proved
    Backtracked,
    Partial, //TODO: partial when tactic and dafny succeed, but boogie fails
  }


  [ContractClassFor(typeof(ISearch))]
  // Validate the input before execution
  public abstract class BaseSearchContract : ISearch {
    public IEnumerable<ProofState> Search(ProofState state, ErrorReporterDelegate er) {
      Contract.Requires(state != null);
      return default(IEnumerable<ProofState>);
    }
  }

  public class BaseSearchStrategy : ISearch{
    public const int BACKTRACK_COUNT_UNDEFINED = -1;

    protected Strategy ActiveStrategy;
    public BaseSearchStrategy(Strategy strategy) {
      ActiveStrategy = strategy;
    }

    protected BaseSearchStrategy() {
    }
    
    public IEnumerable<ProofState> Search(ProofState state, ErrorReporterDelegate er) {
      Contract.Requires<ArgumentNullException>(state != null, "rootState");
	  
	  IEnumerable<ProofState> enumerable;      
      switch (ActiveStrategy) {
        case Strategy.Bfs:
          throw new NotSupportedException("Breath first search has not been supported ");
          //enumerable = BreadthFirstSeach.Search(state, errDelegate);
          break;
        case Strategy.Dfs:
          enumerable = DepthFirstSeach.Search(state, er);
          break;
        case Strategy.Undefined:
          throw new Tcce.UnreachableException();
        default:
          enumerable = DepthFirstSeach.Search(state, er);
          break;
      }
      return enumerable;
    }




    public static VerifyResult VerifyState(ProofState state) {
      // at the momemnt,we don't propagate errors from buanches to user, no need to use errDelegate, in the future this will
      // come to play when repair kicks in
      if (state.IsTimeOut()){
        state.GetErrHandler().ErrType = TacticBasicErr.ErrorType.Timeout;
        return VerifyResult.Failed;
      }
      var prog =  Util.GenerateResolvedProg(state);
      if (prog == null)
      {
        return VerifyResult.Unresolved;
      }

      //   ErrorReporterDelegate tmp_er =
     //   errorInfo => { errDelegate?.Invoke(new CompoundErrorInformation(errorInfo.Tok, errorInfo.Msg, errorInfo, state)); };
      var result = Util.VerifyResolvedProg(state, prog, null);
/*
      ErrorReporterDelegate tmp_er =
        errorInfo => { errDelegate?.Invoke(new CompoundErrorInformation(errorInfo.Tok, errorInfo.Msg, errorInfo, state)); };
      var boogieProg = Util.Translate(prog, prog.Name, tmp_er);
      PipelineStatistics stats;
      List<ErrorInformation> errorList;

      //Console.WriteLine("call verifier in Tacny !!!");
      PipelineOutcome tmp = Util.BoogiePipeline(boogieProg,
        new List<string> { prog.Name }, prog.Name, tmp_er,
        out stats, out errorList, prog);
*/

      if(result)
        return VerifyResult.Verified;
      else {
        return VerifyResult.Failed;
      }
    }
  /*
  public static VerifyResult VerifyState0(ProofState state, ErrorReporterDelegate errDelegate) {
      //TODO: remove body list, only nedd one
      var bodyList = new Dictionary<ProofState, BlockStmt>();
      bodyList.Add(state, Util.InsertCode(state,
        new Dictionary<UpdateStmt, List<Statement>>(){
          {state.TacticApplication, state.GetGeneratedCode()}
        }));
    
        var memberList = Util.GenerateMembers(state, bodyList);
        var prog = Util.GenerateDafnyProgram(state, memberList.Values.ToList());

        Console.WriteLine("*********************Verifying Tacny Generated Lines *****************");
        var printer = new Printer(Console.Out);
      //  printer.PrintProgram(prog, false);
        foreach (var stmt in state.GetGeneratedCode()){
          printer.PrintStatement(stmt,0);
        }
      var result = Util.ResolveAndVerify(prog, errorInfo => { errDelegate?.Invoke(new CompoundErrorInformation(errorInfo.Tok, errorInfo.Msg, errorInfo, state)); });

      Console.WriteLine("\n*********************END*****************");

      if (result)
          return VerifyResult.Verified;
        else {
          //TODO: find which proof state verified (if any)
          //TODO: update verification results
          
          //  errDelegate();
          return VerifyResult.Failed;
        }
      }
      */
  }

 /*
  internal class BreadthFirstSeach : BaseSearchStrategy {

    internal new static IEnumerable<ProofState> Search(ProofState rootState, ErrorReporterDelegate errDelegate){

      var queue = new Queue<IEnumerator<ProofState>>();
      queue.Enqueue(rootState.EvalStep().GetEnumerator());

      IEnumerator<ProofState> enumerator = Enumerable.Empty<ProofState>().GetEnumerator();

      while (queue.Count > 0){
        // check if there is any more item in the enumerartor, if so, MoveNext will move to the next item
        if (!enumerator.MoveNext()){
          // if no item in the current enumerator, pop a new enumerator from the queie, 
          enumerator = queue.Dequeue();
          // set the start point for enumulator, if there is no valid start point, i.e. empty, skip this one
          if (!enumerator.MoveNext())
            continue;
        }
        var proofState = enumerator.Current;
        //check if any new added code reuqires to call the dafny to verity, or reach the last line of code
        if (proofState.NeedVerify || proofState.IsCurFrameEvaluated()) {
          proofState.NeedVerify = false;
          switch (VerifyState(proofState, errDelegate)){
            case VerifyResult.Verified:
              proofState.MarkCurFrameAsTerminated(true);
              if (proofState.IsTerminated()){
                 yield return proofState;
                 yield break;
              }
              //queue.Enqueue(Interpreter.EvalStep(proofState).GetEnumerator());
              break;
            case VerifyResult.Failed:
              if (proofState.IsCurFrameEvaluated()){
                proofState.MarkCurFrameAsTerminated(false);
                if(proofState.IsTerminated()) {
                  yield return proofState;
                  yield break;
                }
              }
              break;
            case VerifyResult.Unresolved:
              //discharge current branch if fails to resolve
              continue;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        if(!proofState.IsCurFrameEvaluated()) {
          queue.Enqueue(proofState.EvalStep().GetEnumerator());
        }
      }
    }
  }
*/


  internal class DepthFirstSeach : BaseSearchStrategy {
  
    internal new static IEnumerable<ProofState> Search(ProofState rootState, ErrorReporterDelegate errDelegate){
      var stack = new Stack<IEnumerator<ProofState>>(); // proof state and  backtrack count
      ProofState lastSucc = null;

      stack.Push(rootState.EvalStep().GetEnumerator());

      //keep failed branched, for the purpose of analyzing failures
      var discarded = new List<Tuple<ProofState, VerifyResult>>();

      IEnumerator<ProofState> enumerator = Enumerable.Empty<ProofState>().GetEnumerator();
      List<int> backtackList = null;

      while(stack.Count > 0) {
        if (!enumerator.MoveNext()){
          enumerator = stack.Pop();
          if (!enumerator.MoveNext())
            continue;
        }
        var proofState = enumerator.Current;
        //set the backtrack list back to the frame, this will udpate the backtrack count for the parent one.
        if (backtackList != null)
          proofState.SetBackTrackCount(backtackList);
        backtackList = proofState.GetBackTrackCount();

        //check if any new added coded reuqires to call verifier, or reach the last line of code
        if(proofState.NeedVerify || proofState.IsCurFrameEvaluated()) {
          proofState.NeedVerify = false;
          bool backtracked = false;

          switch (VerifyState(proofState)){
            case VerifyResult.Verified:
              //check if the frame are evaluated, as well as requiests for backtraking 
              proofState.MarkCurFrameAsTerminated(true, backtracked, out backtracked);
              if (backtracked) {
                lastSucc = proofState;
                discarded.Add(new Tuple<ProofState, VerifyResult>(proofState, VerifyResult.Backtracked));
              }
              
              if (proofState.IsTerminated()){
                yield return proofState;
                yield break;
              }

              //stack.Push(enumerator);
              //enumerator = (Interpreter.EvalStep(proofState).GetEnumerator());
              break;
            case VerifyResult.Failed:
              if(proofState.IsCurFrameEvaluated()) {
                proofState.MarkCurFrameAsTerminated(false, backtracked, out backtracked);
                if (backtracked) {
                  lastSucc = proofState;
                  discarded.Add(new Tuple<ProofState, VerifyResult>(proofState, VerifyResult.Backtracked));
                }
                if (proofState.IsTerminated()) {
                  yield return proofState;
                    yield break;
                  }
                }
              break;
            case VerifyResult.Unresolved:
              discarded.Add(new Tuple<ProofState,VerifyResult>(proofState, VerifyResult.Unresolved));
              //discard current branch if fails to resolve
              continue;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        /*
       * when failed, check if this method is evaluated , i.e. all tstmt are evalauted,
       * if so, do nothing will dischard this branch and continue with the next one
       * otherwise, continue to evaluate the next stmt
       */
        if (!proofState.IsCurFrameEvaluated()){
          //push the current one to the stack
          stack.Push(enumerator);
          //move to the next stmt
          enumerator = (proofState.EvalStep().GetEnumerator());
        }
        else{
          backtackList = proofState.GetBackTrackCount(); // update the current bc count to the list
          if (proofState.InAsserstion)
            proofState.GetErrHandler().ErrType = TacticBasicErr.ErrorType.Assertion;
          else
            proofState.GetErrHandler().ErrType = TacticBasicErr.ErrorType.NotProved;

          discarded.Add(new Tuple<ProofState, VerifyResult>(proofState, VerifyResult.Failed));
        }

      }
      //check if over-backchecked
      if (backtackList != null && backtackList.Exists(x => x > 0)) {
        if (lastSucc == null)
          Console.WriteLine("!!! No more branch for the request of " + (backtackList.Last() + 1) + "backtracking, and no branch.");
        else {
          Console.WriteLine("!!! No more branch for the request of " + lastSucc.GetOrignalTopBacktrack() + ", remaining " + (backtackList.Last() + 1 > lastSucc.GetOrignalTopBacktrack() ? lastSucc.GetOrignalTopBacktrack() : backtackList.Last() + 1) + " requests, return the last one.");
          yield return lastSucc;
        }

      } else {
        // no result is successful
        var s0 = discarded[discarded.Count - 1];
        s0.Item1.GetErrHandler().ExceptionReport();
        if (errDelegate != null) {
          foreach (var err in s0.Item1.GetErrHandler().ErrorList)
          {
            lock (errDelegate)
            {
              errDelegate(new CompoundErrorInformation(
                s0.Item1.GetErrHandler().GenerateErrorMsg(),
                err,
                s0.Item1
                ));
            }
          }
        }
      }
    }
  }
}


