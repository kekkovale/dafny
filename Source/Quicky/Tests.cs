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
        QuickyMain.CreateProgramFromFileName(Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).ToString()) + @"\Test\quicky\"+filename);

      return new Quicky(dafnyProgram);
    }


    //TODO test smaller parts
    [Test]
    public void TestWholeProcess() {
      var quicky = GetQuicky("Test01.dfy");
      quicky.PerformTesting();
      Assert.AreEqual(1, quicky.FoundErrors.Count);
//      foreach (var quickyException in quicky.FoundErrors.Values)
//        System.Diagnostics.Debug.WriteLine("Exception on line "+quickyException.Token.line + " with input: " + quickyException.CounterExamples);
    }
  }
}
