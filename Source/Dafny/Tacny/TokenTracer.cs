using Microsoft.Boogie;
using System;
using System.Collections.Generic;

namespace Microsoft.Dafny.Tacny
{
  public class TokenTracer
  {
    // Tracer := end | (Tracer, Tracer list)
    // to keep track of execution flow

    private static int _default = 0;
    public IToken Origin { get; set; }
    /// <summary>
    /// a trace token will always at -1;
    /// </summary>
    public IToken EndToken{ get; set; }

    // denotes the nth branch igenerated from the code of the Token
    private readonly List<Tuple<IToken, int>> _branchMark; 

    // for calls to frames and those lang stmt e.g. match
    private readonly List<TokenTracer> _subTraces;

    public TokenTracer(IToken origin) {
      this.Origin = origin.Copy();
      _branchMark = new List<Tuple<IToken, int>>();
      EndToken = new Microsoft.Boogie.Token(_default, _default);
      _subTraces = new List<TokenTracer>();
    }

    public void Increase() {
      EndToken.line = EndToken.line - 1;
    }

    public void AddBranchTrace(int idx) {
      _branchMark.Add(new Tuple<IToken, int>(EndToken.Copy(), idx));
    }

    public void AddSubTrace(TokenTracer sub) {
      _subTraces.Add(sub);
    }
    public TokenTracer GenSubTrace() {
      return new TokenTracer(EndToken);
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

      println("Orinal Token: " + Origin.line);
      println("Branch Trace: ["); print("  ");
      var cnt = 0;
      foreach(var item in _branchMark) {
        Console.Write(" ("+item.Item1.line +", " + item.Item2 +")");
        cnt++;
        if (cnt == 8) { cnt = 0; Console.WriteLine("");  print("  "); }
      }
      Console.WriteLine("]");

      println("Sub Trace: [");
      cnt = 1;
      foreach (var item in _subTraces) {
        item.PrettyTrace(space + cnt * 2);
        cnt++;
      }
      println("]");

      println("End Token: " + EndToken.line);
    }

    public void PrettyTrace() {
      Console.WriteLine("|**** Trace of Tokens:");

      PrettyTrace(2);

      Console.WriteLine("End of Trace ****|");
    }

    public List<IToken> GetCallTrace() {
      return null;
    }
    public void PrettyCallTrace() {
    }

    public void PrettyOrigin() {
      Console.Write(Origin.line);
    }
  }
}
