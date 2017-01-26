using System;
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
      QuickyMain.PrintCompiledCode = true;
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
      int i = 0;
      foreach (var method in quicky.FoundErrors.Keys) {
        var quickyException = quicky.FoundErrors[method];
        Console.WriteLine("Exception on line " + quickyException.Token.line + " with input: " +
                                           quickyException.CounterExamples);
        if(i==0) Assert.AreEqual(3, quickyException.Token.line);
        i++;
      }
    }

    [Test]
    public void TestCompiler() {
      var quicky = GetQuicky("Test01.dfy");
      //if no exception is thrown, the test has passed.
    }
  }
}
