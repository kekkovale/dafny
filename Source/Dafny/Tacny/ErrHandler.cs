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
    }
    private readonly TokenTracer _token;
    public ErrorType ErrType { get; set; }
    public List<ErrorInformation> ErrorList { get; set; }
    public static int ReportMode = 0; // 0 for brief mode, 1 for full mode
    public TacticBasicErr(TokenTracer t) {
      _token = t;
    }

    public string GetErrMsg()
    {
      string errMsg = "";
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
          msg = "Tactic can't prove the current VCs: ";
          break;

        default:
          msg = "Fail to apply tactic: ";
          break;
      }
      return msg;
    }

    public void ExceptionReport()
    {
      var err = "";
      err += ("\n================ Tactic Error: ================\n");
      err += ("Fail to apply tactic in line ") + _token.Origin.line + "\n" + (GetErrMsg());
      Console.WriteLine(err);

      switch (ReportMode) {
        case 1:
          _token.PrettyTrace();
          break;
        case 0:
        default:
          break;
      }
      Console.WriteLine("================ End of Tactic Error ================\n");
    }
  }

  public class TacticSyntaxErr : TacticBasicErr
  {
    public TacticSyntaxErr(TokenTracer t) : base(t) {
    }
  }

  public class TacticBranchErr : TacticBasicErr
  {
    public TacticBranchErr(TokenTracer t) : base(t) {
    }
  }

  public class TacticTAserrtErr : TacticBasicErr
  {
    public TacticTAserrtErr(TokenTracer t) : base(t) {
    }
  }



}
