using System;

namespace Microsoft.Dafny.Tacny
{
  public class TacticBasicErr
  {
    private readonly TokenTracer _token;
    public string ErrInfo { get; set; }
    public static int ReportMode = 1; // 0 for brief mode, 1 for full mode
    public TacticBasicErr(TokenTracer t) {
      _token = t;
    }

    public void ExceptionReport() {
      Console.WriteLine("\n================ Tactic Error: ================");
      switch (ReportMode) {
        case 1:
          Console.Write("Fail in applying tactic in line ");
          _token.PrettyOrigin(); Console.WriteLine("");
          Console.WriteLine(ErrInfo);
          _token.PrettyTrace();
          break;
        case 0:
        default:
          Console.Write("Fail in applying tactic in line ");
          _token.PrettyOrigin(); Console.WriteLine("");
          Console.WriteLine(ErrInfo);
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
