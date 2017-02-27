using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Microsoft.Dafny;

namespace Quicky
{
  class UnidentifiedDafnyTypeException : Exception
  {
    public readonly Microsoft.Dafny.Type Type;
    public UnidentifiedDafnyTypeException(Microsoft.Dafny.Type type) : base(CreateMessage(type)) {
      Type = type;
    }

    private static string CreateMessage(Microsoft.Dafny.Type type) {
      string message = "Quicky does not recognise the type: " + type;
      return message;
    }
  }

  //This class exists so different random values are always returned
  static class NextRandomNumberRetriever
  {
    private static readonly Random Random = new Random();

    public static int GetNextRandomNumber(int max) {
      lock (Random) {
        return Random.Next(max);
      }
    }
  }

  class ParameterSetGenerator
  {
    public readonly QuickyChecker QuickyChecker;
    private readonly Method _method;
    private readonly int _fullParamSize; //includes outs (returns part dafny) and quickyChecker

    private readonly List<ParameterGenerator> _parameterGenerators = new List<ParameterGenerator>();
    public int NumParameters => _method.Ins.Count;

    private List<int[]> _indexes = new List<int[]>(); //todo also have a hashset to quickly find? more memory needed but not too much?
    private int _index;
    public readonly int NumTests;


    public ParameterSetGenerator(Quicky quicky, Method method)
    {
      Contract.Requires(quicky != null);
      Contract.Requires(method != null);
      QuickyChecker = new QuickyChecker(method, quicky);
      _method = method;
      _fullParamSize = _method.Ins.Count + _method.Outs.Count + 1; //1 is quickychecker
      NumTests = quicky.TestCases;
      CreateGenerators();
      SetAllCombs();
    }

    private void CreateGenerators() {
      int i = 0;
      foreach (var param in _method.Ins)
        _parameterGenerators.Add(CreateGeneratorOfType(i++, param.SyntacticType));
    }

    private ParameterGenerator CreateGeneratorOfType(int paramNum, Microsoft.Dafny.Type type)
    {
      if (type is IntType)
        return new IntegerGenerator(paramNum, this);
      if(type is BoolType)
        return new BooleanGenerator(paramNum, this);
      throw new UnidentifiedDafnyTypeException(type); //TODO catch it - do not test that method
    }

    public object[] GetNextParameterSet() {
      object[] parameters = new object[_fullParamSize];
      parameters[0] = QuickyChecker;
      for (int i = 0; i < _parameterGenerators.Count; i++)
        parameters[i+1] = _parameterGenerators[i].GetNextParameter(_indexes[_index][i]);
      for (int i = _method.Ins.Count + 1; i < _fullParamSize; i++) //required for outs
        parameters[i] = null;
      _index++;
      return parameters;
    }

    public static int Sum(int[] vals) {
      int total = 0;
      foreach (int t in vals) {
        total += t;
      }
      return total;
    }

    public void SetCombinations(int[] arr, int len, int startPos, int[] results)
    {
      if (len == 0) {
        //create new instance of results so the result is not coppied to other ones
        _indexes.Add((int[]) results.Clone());
        return;
      }
      for (int i = startPos; i <= arr.Length - len; i++) {
        results[results.Length - len] = arr[i];
        SetCombinations(arr, len-1, i+1, results);
      }
    }


    private void SetAllCombs() {
      //make a 0 array of size
      _indexes = new List<int[]>(NumParameters);
      _indexes.Add(new int[NumParameters]);
      for (int i = 0; i < NumParameters; i++) {
        _indexes[0][i] = 0;
      }
      for (int i = 0; i < NumTests; i++) {
//        Contract.Requires(_indexes.Count > i);
        SetComb(_indexes[i]);
      }
    }
    
    private void SetComb(int[] prev) {
      for (int i = 0; i < prev.Length; i++) {
        if (_indexes.Count >= NumTests) return;
        int[] newContainer = (int[]) prev.Clone();
        newContainer[i]++;
        if (!_indexes.Any(newContainer.SequenceEqual))
          _indexes.Add(newContainer);
      }
    }

  }
  
  abstract class ParameterGenerator
  {
    protected ParameterSetGenerator ParamSetGenerator;
    protected int ParamNum;

    protected object[] CachedValues;

    protected ParameterGenerator(int paramNum, ParameterSetGenerator parameterSetGenerator) {
      Contract.Requires(paramNum < parameterSetGenerator.NumParameters);
      ParamSetGenerator = parameterSetGenerator;
      ParamNum = paramNum;
      CachedValues = new object[parameterSetGenerator.NumTests];
    }

    public object GetNextParameter(int index) {
      return CachedValues[index] ?? (CachedValues[index] = GetNextItem(index));
    }

    protected abstract object GetNextItem(int index);
  }

  class IntegerGenerator : ParameterGenerator
  {
    public IntegerGenerator(int paramNum, ParameterSetGenerator parameterSetGenerator) : base(paramNum, parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      switch (index) {
        case 0: return new BigInteger(1);
        case 1: return new BigInteger(0);
        case 2: return new BigInteger(-1);
        case 3: return new BigInteger(10);
      }
      //TODO need to get a range of random numbers depending on index and number of test cases (will also be defined by number of parameters) 
      int value = NextRandomNumberRetriever.GetNextRandomNumber(5000);
      return new BigInteger(value);
    }
  }

  class BooleanGenerator : ParameterGenerator
  {
    public BooleanGenerator(int paramNum, ParameterSetGenerator parameterSetGenerator) : base(paramNum, parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      return index % 2 == 0;
    }
  }
}
