using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;

namespace Quicky
{
  //todo: move error info into here?  can be tracked alongside precondition fails count
  //This class is referenced from quicky compiled programs to check and react to 
  public class QuickyChecker
  {
   

    private readonly Method _method;
    private readonly Quicky _quicky;

    
    public int PreconditionFails { get; private set; }

    public QuickyChecker(Method method, Quicky quicky)
    {
      _method = method;
      _quicky = quicky;
    }

    public void PreconditionFailed()
    {
      PreconditionFails++;
    }

    public void TrackError(int lineNum, int columnNum, string counterExamples, QuickyError.ErrorType errorType) {
      Microsoft.Boogie.Token tok = new Microsoft.Boogie.Token(lineNum, columnNum);
      var exception = new QuickyError(tok, counterExamples, errorType);
      if (!_quicky.FoundErrors.ContainsKey(_method))
        _quicky.FoundErrors.Add(_method, exception);
    }
  }
}
