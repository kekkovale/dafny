using Microsoft.Boogie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny
{
  public class TokenTracer
  {
    // Tracer := end | (Tracer, Tracer list)
    // to keep track of execution flow

    private static int _default = 0;
    public IToken origin { get; set; }
    /// <summary>
    /// a trace token will always at -1;
    /// </summary>
    public IToken endToken{ get; set; }

    // denotes the nth branch igenerated from the code of the Token
    private List<Tuple<IToken, int>> branchMark; 

    // for calls to frames and those lang stmt e.g. match
    private List<TokenTracer> SubTraces;

    public TokenTracer(IToken origin) {
      this.origin = origin.Copy();
      branchMark = new List<Tuple<IToken, int>>();
      endToken = new Microsoft.Boogie.Token(_default, _default);
      SubTraces = new List<TokenTracer>();
    }

    public void Increase() {
      endToken.line = endToken.line - 1;
    }

    public void AddBranchTrace(int idx) {
      branchMark.Add(new Tuple<IToken, int>(endToken.Copy(), idx));
    }

    public void AddSubTrace(TokenTracer sub) {
      SubTraces.Add(sub);
    }
    public TokenTracer GenSubTrace() {
      return new TokenTracer(endToken);
    }

    internal string ToNSpace(int cnt) {
      string ret = "";
      for(int i = 0; i < cnt; i++) {
        ret = ret + " ";
      }
      return ret;
    }

    internal void PrettyTrace(int space) {
      var tab = ToNSpace(space);
      Action<String> print = x => Console.Write(tab + x);
      Action<String> println = x => Console.WriteLine(tab + x);

      println("Orinal Token: " + origin.line);
      println("Branch Trace: ["); print("  ");
      var cnt = 0;
      foreach(var item in branchMark) {
        Console.Write(" ("+item.Item1.line +", " + item.Item2 +")");
        cnt++;
        if (cnt == 8) { cnt = 0; Console.WriteLine("");  print("  "); }
      }
      Console.WriteLine("]");

      println("Sub Trace: [");
      cnt = 1;
      foreach (var item in SubTraces) {
        item.PrettyTrace(space + cnt * 2);
        cnt++;
      }
      println("]");

      println("End Token: " + endToken.line);
    }

    public void PrettyTrace() {
      Console.WriteLine("|**** Trace of Tokens:");

      PrettyTrace(2);

      Console.WriteLine("End of Trace ****|");
    }
  }
}
