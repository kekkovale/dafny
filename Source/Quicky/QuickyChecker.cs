﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;
using Bpl=Microsoft.Boogie;

namespace Quicky
{
  //todo: move error info into here?  can be tracked alongside precondition fails count
  //This class is referenced from quicky compiled programs to check and react to 
  public class QuickyChecker
  {
    protected internal readonly Method Method;
    protected internal readonly Quicky Quicky;
    
    public int PreconditionFails { get; private set; }

    public QuickyChecker(Method method, Quicky quicky) {
      Contract.Requires(quicky != null);
      Method = method;
      Quicky = quicky;
    }

    public virtual void PreconditionFailed() {
      PreconditionFails++;
    }

    public virtual void TrackError(int lineNum, int columnNum, string counterExamples, QuickyError.ErrorType errorType) {
      Microsoft.Boogie.Token tok = new Microsoft.Boogie.Token(lineNum, columnNum);
      string implName = "Impl$$" + Method.FullSanitizedName;
      var exception = new QuickyError(tok, counterExamples, errorType, implName);
      if (!Quicky.FoundErrors.ContainsKey(Method))
        Quicky.FoundErrors.Add(Method, exception);
    }
  }
  //TODO add this to compiler
  /// <summary>
  /// The nested quickyChecker is created for when another function is called.  It will act differently due and assing the error
  /// to the correct method deppending on if it was a precondition failure or otherwise.
  /// </summary>
  public class NestedQuickyChecker : QuickyChecker
  {
    private readonly QuickyChecker _originalChecker;
    private readonly int _line;
    private readonly int _col;
    private readonly string _counterExample;

    public NestedQuickyChecker(QuickyChecker quickyChecker, int line, int col, string counterExample) : base(quickyChecker.Method, quickyChecker.Quicky) {
      _originalChecker = quickyChecker;
      _line = line;
      _col = col;
      _counterExample = counterExample;
    }

    public override void PreconditionFailed() {
      //Precondition for the callMethod failed: this is an issue with the ORIGINAL method! call was from _line and _col values!
      _originalChecker.TrackError(_line, _col, _counterExample, QuickyError.ErrorType.PreconditionCall);
    }

    public override void TrackError(int lineNum, int columnNum, string counterExamples, QuickyError.ErrorType errorType) {
      //An error has occured in the callMethod, despite having passed the preconditions.  issue is in the CALLMETHOD
      
      //For now, this is simply being ignored: hopefully the errors will be found when that method is tested (with better parameters)
    }

  }

  public class QuickyError
  {
    public enum ErrorType
    {
      Postcondition,
      Assert,
      InvariantEntry,
      InvariantEnd,
      PreconditionCall
    }

    //Error messages to be displayed for certain types of failures
    private static readonly Dictionary<ErrorType, string> ErrorMessages = new Dictionary<ErrorType, string>() {
      {ErrorType.Postcondition, "Postcondition failed"},
      {ErrorType.Assert, "Assert failed"},
      {ErrorType.InvariantEntry, "Invariant failed on entry"},
      {ErrorType.InvariantEnd, "Invariant failed at the end of a loop iteration"},
      {ErrorType.PreconditionCall, "Precondition was not valid for method call"}
    };

    public ErrorType TypeOfError;
    public Bpl.IToken Token; //TODO remove token, just use line and col num?
    public string CounterExamples;
    public string ImplementationName;

    public string Message => ErrorMessages[TypeOfError] + " with parameters: " + CounterExamples;

    public QuickyError(Bpl.IToken token, string counterExamples, ErrorType errorType, string impName) {
      Token = token;
      CounterExamples = counterExamples;
      TypeOfError = errorType;
      ImplementationName = impName;
    }
  }
}
