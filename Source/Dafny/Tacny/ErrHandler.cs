using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny
{
  public class TacticBasicErr
  {
    public enum ErrorType
    {
      Timeout,
      Backtracked,
      SyntaxErr,
      TypeErr, /* type error for argument*/
      SemanticErr, /* Fail to eval a stmt, e.g. p:| post_conds() when no postconditions*/
      Assertion,
      NotProved,
      Deafult
    }
    public ErrorType ErrType { get; set; }
    public List<ErrorInformation> ErrorList { get; set; }
    public ErrorReporter Reporter;
    public static int ReportMode = 0; // 0 for brief mode, 1 for full mode
    public TacticBasicErr() {
      ErrType = ErrorType.Deafult;
      Reporter = new ConsoleErrorReporter();
    }

    public void ClearErrMsg()
    {
      if (Reporter.Count(ErrorLevel.Error) != 0) {
        Reporter = new ConsoleErrorReporter();
      }

      if (ErrorList != null && ErrorList.Count > 0) {
        ErrorList = null;
      }
    }
    public string GetErrMsg()
    {
      string errMsg = "";
      if (ErrorList == null){
        return "Fail to apply tactic!";
      }
      foreach (var msg in ErrorList) {
        errMsg = errMsg + "\n " + msg.FullMsg;
      }
      return errMsg;
    }

    public string GenerateErrorMsg()
    {
      string msg;
      switch (ErrType)
      {
        case ErrorType.Timeout:
          msg = "Tactic is timeouted: ";
          break;

        case ErrorType.Backtracked:
          msg = "A bracktracked branch, users are not supporsed to see this message.";
          break;

        case ErrorType.SyntaxErr:
          msg = "Syntax errors: ";
          break;

        case ErrorType.Assertion:
          msg = "Tactic assertion: ";
          break;

        case ErrorType.SemanticErr:
          msg = "Fail to evaluste a tactic statement: ";
          break;
   
        case ErrorType.NotProved:
          msg = "Tactic can't prove it: ";
          break;

        default:
          msg = "Fail to apply tactic: ";
          break;
      }
      return msg;
    }

  }
  
  public class CompoundErrorInformation : ErrorInformation
  {
    public readonly ProofState S;

    private static string StringOfStmt(ProofState state) {
      var writer = new System.IO.StringWriter();
      var r = new Printer(writer);
      r.PrintStatement(state.GetLastStmt(), 0);

      var str = writer.ToString();
      // Console.WriteLine(str);
      return str;
    }

    private CompoundErrorInformation(string msg, ProofState state)
      : base(new Token(Interpreter.TacticCodeTokLine, Interpreter.TacticCodeTokLine),
          msg == "" ? " Exception in: " + StringOfStmt(state) : msg) {

      ImplementationName = "Impl$$" + state.TargetMethod.FullName;
      S = state;
    }

    private CompoundErrorInformation(string msg, ErrorInformation e, ProofState s)
      : base(s.TopLevelTacApp.Tok, msg + " " + e.FullMsg) {
      this.ImplementationName = e.ImplementationName;
      S = s;
    }

    public static List<CompoundErrorInformation> GenerateErrorInfoList(ProofState state, string msg = "") {
      List<CompoundErrorInformation> errs = new List<CompoundErrorInformation>();
      //resolving errors: moving those errors to error info
      var report = state.GetErrHandler().Reporter;
      if (report.Count(ErrorLevel.Error) != 0) {
        foreach (var errMsg in report.AllMessages[ErrorLevel.Error]) {
          AddErrorInfo(state, errMsg.message);
        }
      }
      // verification error + resolving errors
      var l = state.GetErrHandler().ErrorList;
      Console.WriteLine("\n================ Tactic exception: ================");

      if (l != null && l.Count > 0) {
        foreach (var err in l) {
          errs.Add(new CompoundErrorInformation(msg, err, state));
          Console.WriteLine(err.FullMsg);
        }
      }
      var errInfo = new CompoundErrorInformation(msg, state);
      Console.WriteLine(errInfo.FullMsg);
      Console.WriteLine("================ End of tactic exception ================");
      errs.Add(errInfo);

      return errs;
    }

    internal static void AddErrorInfo(ProofState state, string msg) {
      var errInfo = new CompoundErrorInformation(msg, state);
      if (state.GetErrHandler().ErrorList == null)
        state.GetErrHandler().ErrorList = new List<ErrorInformation>();
      state.GetErrHandler().ErrorList.Add(errInfo);
    }
  }




}
