using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Dafny;
using Microsoft.Dafny;
using Type = Microsoft.Dafny.Type;

namespace Quicky
{
  public class UnidentifiedDafnyTypeException : Exception
  {
    public readonly List<Type> Types;
    public UnidentifiedDafnyTypeException(List<Type> types) : base(CreateMessage(types)) {
      Types = types;
    }

    private static string CreateMessage(List<Type> types) {
      string message = "Quicky does not recognise the types: ";
      for (int i = 0; i < types.Count; i++) {
        message += types[i];
        if(i < types.Count-1)
          message += ", ";
      }
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

    public static int GetNextRandomNumber(int min, int max) {
      lock (Random) {
        return Random.Next(min, max);
      }
    }
  }

  /// <summary>
  /// Given a method, this has the ability to return a set of parameters that would run through the function
  /// </summary>
  public class ParameterSetGenerator
  {
    public readonly QuickyChecker QuickyChecker;
    private readonly Method _method;
    private readonly int _fullParamSize; //includes outs (returns part dafny) and quickyChecker

    private readonly List<ParameterGenerator> _parameterGenerators = new List<ParameterGenerator>();
    public int NumParameters => _method.Ins.Count;

    private List<int[]> _indexes = new List<int[]>(); //todo also have a hashset to quickly find? more memory needed but not too much?
    private int _index;
    public readonly int NumTests;

    private List<Type> invalidTypes = new List<Type>();


    public ParameterSetGenerator(Quicky quicky, Method method)
    {
      Contract.Requires(quicky != null);
      Contract.Requires(method != null);
      QuickyChecker = new QuickyChecker(method, quicky);
      _method = method;
      _fullParamSize = _method.Ins.Count + _method.Outs.Count + 1; //1 is quickychecker
      NumTests = quicky.TestCases;
      CreateGenerators();
      SetAllCombinations();
    }

    private void CreateGenerators() {
      foreach (var param in _method.Ins)
        _parameterGenerators.Add(CreateGeneratorOfType(param.SyntacticType));
      if(invalidTypes.Count > 0)
        throw new UnidentifiedDafnyTypeException(invalidTypes);
    }

    private ParameterGenerator CreateGeneratorOfType(Microsoft.Dafny.Type type)
    {
      if (type is IntType)
        return new IntegerGenerator(this);
      if(type is BoolType)
        return new BooleanGenerator(this);
      if (type is CharType)
        return new CharGenerator(this);
      if (type is RealType)
        return new RealGenerator(this);
      if (type.IsArrayType) {
        ArrayClassDecl decl = type.AsArrayType;
        Contract.Assert(decl != null);
        Microsoft.Dafny.Type subType = UserDefinedType.ArrayElementType(type);
        if (!subType.IsArrayType)
          return new ArrayGenerator(this, CreateGeneratorOfType(subType));
      }
      else if (type is UserDefinedType) { //TODO look into this more
        var userType = (UserDefinedType) type;
        if (userType.Name == "nat") { //becomes BigInteger, must be >=0
          return new NatGenerator(this);
        }
      }
      else if (type is CollectionType) {
        var collectionType = (CollectionType) type;
        var subType = collectionType.Arg;
        if (subType.IsArrayType || subType.IsISetType || subType.AsSeqType != null) {
          invalidTypes.Add(type);
          return null;
        }
        if (collectionType.AsSetType != null)
          return new SetGenerator(this, CreateGeneratorOfType(subType));
        if (collectionType.AsSeqType != null)
          return new SequenceGenerator(this, CreateGeneratorOfType(subType));
      }

      /* Other types todo:
       * BitVectorType, ObjectType
       * userDefinedTypes -> arrays
       * ArrowType
       * 
       * CollectionTypes: SetType, MultiSetType, SeqType, MapType
       *  These are defines as AsCollectionType then As{whatever}
       * 
       * 
       * SelfType
       * ArtificialTypes: IntVarietiesSuperType, RealVarietiesSuperType
       * ParamTypeProxy, InferedTypeProxy
       * 
       */

      invalidTypes.Add(type);
      return null;
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

    private void SetAllCombinations() {
      //make a 0 array of size
      _indexes = new List<int[]>(NumParameters) {new int[NumParameters]};
      for (int i = 0; i < NumParameters; i++) {
        _indexes[0][i] = 0;
      }
      for (int i = 0; i < NumTests; i++) {
        SetCombination(_indexes[i]);
      }
    }
    
    private void SetCombination(int[] prev) {
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
    protected object[] CachedValues;

    protected ParameterGenerator(ParameterSetGenerator parameterSetGenerator) {
      CachedValues = new object[parameterSetGenerator.NumTests];
    }

    public object GetNextParameter(int index) {
      return CachedValues[index] ?? (CachedValues[index] = GetNextItem(index));
    }

    protected abstract object GetNextItem(int index);

    public abstract object GetArrayFilledWith(int[] indexes);
    public abstract object GetSetFilledWith(int[] items);
    public abstract object GetSequenceFilledWith(int[] items);
  }


  internal class RealGenerator : ParameterGenerator
  {
    public RealGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      //to get this to work, BigRational had to be declared outside of dafnyruntime and QuickyRuntime was created
      switch (index) {
        case 0: return new BigRational(1);
        case 1: return new BigRational(0);
        case 2: return new BigRational(-1);
        case 3: return new BigRational(1,2);
      }
      int numerator = NextRandomNumberRetriever.GetNextRandomNumber(5000);
      int denominator = NextRandomNumberRetriever.GetNextRandomNumber(5000);
      return new BigRational(numerator, denominator);
    }

    public override object GetArrayFilledWith(int[] indexes) {
      return ArrayFiller.FillArray<BigRational>(indexes, this);
    }

    public override object GetSetFilledWith(int[] items) {
      return ArrayFiller.GetSet<BigRational>(items, this);
    }

    public override object GetSequenceFilledWith(int[] items) {
      return ArrayFiller.GetSequence<BigRational>(items, this);
    }
  }

  internal class NatGenerator : ParameterGenerator
  {
    public NatGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {
      
    }

    protected override object GetNextItem(int index) {
      switch (index) {
        case 0: return new BigInteger(0);
        case 1: return new BigInteger(1);
        case 2: return new BigInteger(10);
      }
      return new BigInteger(NextRandomNumberRetriever.GetNextRandomNumber(5000));
    }

    public override object GetArrayFilledWith(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
    }

    public override object GetSetFilledWith(int[] items) {
      return ArrayFiller.GetSet<BigInteger>(items, this);
    }

    public override object GetSequenceFilledWith(int[] items) {
      return ArrayFiller.GetSequence<BigInteger>(items, this);
    }
  }

  internal class CharGenerator : ParameterGenerator
  {
    public CharGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      return (char) NextRandomNumberRetriever.GetNextRandomNumber(256);
    }

    public override object GetArrayFilledWith(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
    }

    public override object GetSetFilledWith(int[] items) {
      return ArrayFiller.GetSet<BigInteger>(items, this);
    }

    public override object GetSequenceFilledWith(int[] items) {
      return ArrayFiller.GetSequence<BigInteger>(items, this);
    }
  }

  class IntegerGenerator : ParameterGenerator
  {
    public IntegerGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      switch (index) {
        case 0: return new BigInteger(1);
        case 1: return new BigInteger(0);
        case 2: return new BigInteger(-1);
        case 3: return new BigInteger(10);
      }
      //TODO need to get a range of random numbers depending on index and number of test cases (will also be defined by number of parameters) 
      int value = NextRandomNumberRetriever.GetNextRandomNumber(-5000, 5000);
      return new BigInteger(value);
    }

    public override object GetArrayFilledWith(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
    }

    public override object GetSetFilledWith(int[] items) {
      return ArrayFiller.GetSet<BigInteger>(items, this);
    }

    public override object GetSequenceFilledWith(int[] items) {
      return ArrayFiller.GetSequence<BigInteger>(items, this);
    }
  }

  class BooleanGenerator : ParameterGenerator
  {
    public BooleanGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      return index % 2 == 0;
    }

    public override object GetArrayFilledWith(int[] indexes) {
      return ArrayFiller.FillArray<bool>(indexes, this);
    }

    public override object GetSetFilledWith(int[] items) {
      return ArrayFiller.GetSet<bool>(items, this);
    }

    public override object GetSequenceFilledWith(int[] items) {
      return ArrayFiller.GetSequence<bool>(items, this);
    }
  }

  //TODO create generator for things with subTypes?
  class ArrayGenerator : ParameterGenerator
  {
    private readonly ParameterGenerator _itemGenerator;
    public ArrayGenerator(ParameterSetGenerator parameterSetGenerator, ParameterGenerator itemGenerator): base(parameterSetGenerator) {
      _itemGenerator = itemGenerator;
    }

    protected override object GetNextItem(int index) {
      if (index == 0)
        return _itemGenerator.GetArrayFilledWith(new int[0]);
      if (index < 3) // array of size 1 containing first 2 item cases
        return _itemGenerator.GetArrayFilledWith(new[] {index-1});
      if (index < 6) //arrays of size 2
        return _itemGenerator.GetArrayFilledWith(new[] {index - 3, index - 2});
      //random size of array
      int arraySize = NextRandomNumberRetriever.GetNextRandomNumber(100);
      int[] array = new int[arraySize];
      for (int i = 0; i < arraySize; i++) {
        array[i] = NextRandomNumberRetriever.GetNextRandomNumber(99); //TODO: make this number of tests!!!
      }
      return _itemGenerator.GetArrayFilledWith(array);
    }

    public override object GetArrayFilledWith(int[] indexes) {
        //TODO need to figure out what to do about nested arrays
        throw new NotImplementedException("Nested empty array generation not yet working due to types");
    }

    public override object GetSetFilledWith(int[] items) {
      throw new NotImplementedException();
    }

    public override object GetSequenceFilledWith(int[] items) {
      throw new NotImplementedException();
    }
  }

  class SetGenerator : ParameterGenerator
  {
    private readonly ParameterGenerator _itemGenerator;

    public SetGenerator(ParameterSetGenerator parameterSetGenerator, ParameterGenerator itemGenerator) : base(parameterSetGenerator) {
      _itemGenerator = itemGenerator;
    }
    
    protected override object GetNextItem(int index) {
      //TODO do not try to create multiple with the same items! will yeild the same set (or do same for array)
      if (index == 0)
        return _itemGenerator.GetSetFilledWith(new int[0]);
      if (index < 3) // array of size 1 containing first 2 item cases
        return _itemGenerator.GetSetFilledWith(new[] { index - 1 });
      if (index < 6) //arrays of size 2
        return _itemGenerator.GetSetFilledWith(new[] { index - 3, index - 2 });
      //random size of array
      int setSize = NextRandomNumberRetriever.GetNextRandomNumber(100);
      int[] array = new int[setSize];
      for (int i = 0; i < setSize; i++) {
        array[i] = NextRandomNumberRetriever.GetNextRandomNumber(99); //TODO: make this number of tests!!!
      }
      return _itemGenerator.GetSetFilledWith(array);
    }

    public override object GetArrayFilledWith(int[] indexes) {
      throw new NotImplementedException();
    }

    public override object GetSetFilledWith(int[] items) {
      throw new NotImplementedException();
    }

    public override object GetSequenceFilledWith(int[] items) {
      throw new NotImplementedException();
    }
  }

  internal class SequenceGenerator : ParameterGenerator
  {
    private readonly ParameterGenerator _itemGenerator;

    public SequenceGenerator(ParameterSetGenerator parameterSetGenerator, ParameterGenerator itemGenerator) : base(parameterSetGenerator) {
      _itemGenerator = itemGenerator;
    }
    
    protected override object GetNextItem(int index) {
      if (index == 0)
        return _itemGenerator.GetSequenceFilledWith(new int[0]);
      if (index < 3) // array of size 1 containing first 2 item cases
        return _itemGenerator.GetSequenceFilledWith(new[] { index - 1 });
      if (index < 6) //arrays of size 2
        return _itemGenerator.GetSequenceFilledWith(new[] { index - 3, index - 2 });
      //random size of array
      int setSize = NextRandomNumberRetriever.GetNextRandomNumber(100);
      int[] array = new int[setSize];
      for (int i = 0; i < setSize; i++) {
        array[i] = NextRandomNumberRetriever.GetNextRandomNumber(99); //TODO: make this number of tests!!!
      }
      return _itemGenerator.GetSequenceFilledWith(array);
    }

    public override object GetArrayFilledWith(int[] indexes) {
      throw new NotImplementedException();
    }

    public override object GetSetFilledWith(int[] items) {
      throw new NotImplementedException();
    }

    public override object GetSequenceFilledWith(int[] items) {
      throw new NotImplementedException();
    }
  }

  class ArrayFiller
  {
    public static T[] FillArray<T>(int[] indexes, ParameterGenerator generator) {
      T[] array = new T[indexes.Length];
      for (int i = 0; i < indexes.Length; i++)
        array[i] = (T) generator.GetNextParameter(indexes[i]);
      return array;
    }

    public static Set<T> GetSet<T>(int[] items, ParameterGenerator generator) {
      return Set<T>.FromElements(FillArray<T>(items, generator));
    }

    public static Sequence<T> GetSequence<T>(int[] items, ParameterGenerator generator) {
      return Sequence<T>.FromElements(FillArray<T>(items, generator));
    }
  }
}
