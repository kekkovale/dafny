using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;

namespace Quicky
{
  class UnidentifiedDafnyTypeException : Exception
  {
    public Microsoft.Dafny.Type Type;
    public UnidentifiedDafnyTypeException(Microsoft.Dafny.Type type) : base() {
      //TODO properly set message from type
      Type = type;
    }
  }

  class ParameterSetGenerator
  {
    public readonly QuickyChecker QuickyChecker;
    private readonly Method _method;
    private readonly int _fullParamSize; //includes outs (returns part dafny) and quickyChecker

    private readonly List<ParameterGenerator> _parameterGenerators = new List<ParameterGenerator>();

    public ParameterSetGenerator(Quicky quicky, Method method)
    {
      QuickyChecker = new QuickyChecker(method, quicky);
      _method = method;
      _fullParamSize = _method.Ins.Count + _method.Outs.Count + 1; //1 is quickychecker
      CreateGenerators();
    }

    private void CreateGenerators() {
      foreach (var param in _method.Ins)
        _parameterGenerators.Add(CreateGeneratorOfType(param.SyntacticType));
    }

    private ParameterGenerator CreateGeneratorOfType(Microsoft.Dafny.Type type)
    {
      if (type is IntType)
        return new IntegerGenerator();
      if(type is BoolType)
        return new BooleanGenerator();
      throw new UnidentifiedDafnyTypeException(type); //TODO catch it - do not test that method
    }

    public object[] GetNextParameterSet() {
      object[] parameters = new object[_fullParamSize];
      parameters[0] =  QuickyChecker;
      for (int i = 0; i < _parameterGenerators.Count; i++)
        parameters[i+1] = _parameterGenerators[i].GetNextParameter();
      for (int i = _method.Ins.Count + 1; i < _fullParamSize; i++) //required for outs
        parameters[i] = null;
      return parameters;
    }
  }

  abstract class ParameterGenerator
  {
    protected int Index; //TODO make it so this will interact with other parameters when updated:
    //e.g. tests for Test(int a, int b) will yeild (0,0), (0,1), (1,0) and (1,1)

    public object GetNextParameter() {
      Index++; //TODO smarter index stuff
      return GetNextItem();
    }
    protected abstract object GetNextItem();
    
  }

  class IntegerGenerator : ParameterGenerator
  {
    protected override object GetNextItem() {
      Random random = new Random();
      int value = random.Next(5000);//TODO: start with smaller numbers and work up somehow so simple examples are given OR integrate fscheck for generation only
      return new BigInteger(value);
    }
  }

  class BooleanGenerator : ParameterGenerator
  {
    protected override object GetNextItem() {
      return Index % 2 == 0;
    }
  }
}
