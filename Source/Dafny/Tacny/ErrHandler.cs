using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Boogie;
using System.Diagnostics.Contracts;

namespace Microsoft.Dafny.Tacny
{
  public class TacticBasicErr
  {
    private TokenTracer token;
    public string ErrInfo { get; set; }
    public static int reportMode = 0; // 0 for brief mode, 1 for full mode
    public TacticBasicErr(TokenTracer t) {
      this.token = t;
    }

    public void ExceptionReport(int mode) {
      Console.WriteLine("\n================ Tactic Error: ================");
      switch (mode){
        case 1:
          token.PrettyTrace();
          break;
        case 0:
        default:
          Console.Write("Fail in applying tactic in line ");
          token.PrettyOrigin(); Console.WriteLine("");
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
