using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
      
      Resolver resolver = new Resolver(dafnyProgram);
      resolver.ResolveProgram(dafnyProgram);
      return new Quicky(dafnyProgram);
    }
    
    [Test]
    public void TestCompiler() {
      var quicky = GetQuicky("VariousFails.dfy");
      //if no exception is thrown, the program successfully compiled something.
    }
    

    //Runs a file and ensures that the program finds the right errors - the dictionary should contain counts for types of errors
    public void TestForNErrors(string filename, Dictionary<QuickyError.ErrorType, int> expectedErrors) {
      var quicky = GetQuicky(filename);
      quicky.PerformTesting();

      Dictionary<QuickyError.ErrorType, int> foundErrors = new Dictionary<QuickyError.ErrorType, int>();
      foreach (var quickyError in quicky.FoundErrors.Values) {
        Console.WriteLine("Exception on line " + quickyError.Token.line + ": ");
        Console.WriteLine(quickyError.Message+"\n");
        if (foundErrors.ContainsKey(quickyError.TypeOfError))
          foundErrors[quickyError.TypeOfError]++;
        else
          foundErrors.Add(quickyError.TypeOfError, 1);
      }

      foreach (var error in expectedErrors.Keys) {
        Assert.AreEqual(expectedErrors[error], foundErrors[error], "Incorrect number of errors of type "+error);
      }
    }

    [Test]
    public void TestPostConditionFail() {
      string filename = "PostConditionFail.dfy";
      var errorCounts = new Dictionary<QuickyError.ErrorType, int>() {
        {QuickyError.ErrorType.Postcondition, 1}
      };
      TestForNErrors(filename, errorCounts);
    }

    [Test]
    public void TestAssertFail() {
      string filename = "AssertFail.dfy";
      var errorCounts = new Dictionary<QuickyError.ErrorType, int>() {
        {QuickyError.ErrorType.Assert, 1}
      };
      TestForNErrors(filename, errorCounts);
    }

    [Test]
    public void LoopInvariantEntryFail() {
      string filename = "LoopEntryFail.dfy";
      var errorCounts = new Dictionary<QuickyError.ErrorType, int>() {
        {QuickyError.ErrorType.InvariantEntry, 1}
      };
      TestForNErrors(filename, errorCounts);
    }

    [Test]
    public void TestLoopInvariantEndFail() {
      string filename = "LoopEndFail.dfy";
      var errorCounts = new Dictionary<QuickyError.ErrorType, int>() {
        {QuickyError.ErrorType.InvariantEnd, 1}
      };
      TestForNErrors(filename, errorCounts);
    }

    [Test]
    public void TestVariousFailures() {
      string fileName = "VariousFails.dfy";
      QuickyMain.PrintCompiledCode = true;
      var errorCounts = new Dictionary<QuickyError.ErrorType, int>() {
        {QuickyError.ErrorType.Postcondition, 1},
        {QuickyError.ErrorType.Assert, 1},
        {QuickyError.ErrorType.InvariantEntry, 1},
        {QuickyError.ErrorType.InvariantEnd, 1}
      };
      TestForNErrors(fileName, errorCounts);
    }
  }
}
