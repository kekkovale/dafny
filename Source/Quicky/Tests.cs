using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;
using NUnit.Framework;

namespace Quicky
{

  [TestFixture]
  class Tests
  {
    private Quicky GetQuicky(string filename) {
      QuickyMain.SetupEnvironment();
      Program dafnyProgram =
        QuickyMain.CreateProgramFromFileName("C:\\Users\\Duncan\\Dissertation\\Quicky\\Test\\quicky\\"+filename);

      return new Quicky(dafnyProgram);
    }

    [Test]
    public void TestOutputs() {
      var quicky = GetQuicky("Test01.dfy");
      quicky.PerformTesting();
      //Assert.AreEqual(3, (int)(BigInteger)quicky.Outputs[0]);
      foreach (var quickyException in quicky.FoundErrors.Values) {
        Console.WriteLine("Exception on line "+quickyException.Token.line + " with input: " + quickyException.CounterExamples);
      }
    }

    [Test]
    public void TestPostConditionFailure() {
      
    }
  }
}
