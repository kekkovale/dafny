using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;
using NUnit.Framework;
using System.IO;

namespace Quicky
{

  [TestFixture]
  class Tests
  {
    private Quicky GetQuicky(string filename) {
      QuickyMain.SetupEnvironment();
      Program dafnyProgram =
        QuickyMain.CreateProgramFromFileName(Directory.GetParent(QuickyMain.BinariesDirectory()) + @"\Test\quicky\"+filename);

      return new Quicky(dafnyProgram);
    }
    
    //TODO test smaller parts
    [Test]
    public void TestWholeProcess() {
      var quicky = GetQuicky("Test01.dfy");
      quicky.PerformTesting();
      Assert.AreEqual(3, quicky.FoundErrors.Count);
      foreach (var quickyException in quicky.FoundErrors.Values) {
        Console.WriteLine("Exception on line " + quickyException.Token.line + " with input: " +
                                           quickyException.CounterExamples);
        Assert.AreEqual(3, quickyException.Token.line);
        break;
      }
    }
  }
}
