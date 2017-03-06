using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Dafny;
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

  /// <summary>
  /// Given a method, this has the ability to return a set of parameters that would run through the function
  /// </summary>
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
      foreach (var param in _method.Ins)
        _parameterGenerators.Add(CreateGeneratorOfType(param.SyntacticType));
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
      if (type.IsArrayType)
      {
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

      /* Other types todo:
       * BitVectorType, ObjectType
       * userDefinedTypes -> arrays
       * ArrowType
       * CollectionTypes: SetType, MultiSetType, SeqType, MapType
       * SelfType
       * ArtificialTypes: IntVarietiesSuperType, RealVarietiesSuperType
       * ParamTypeProxy, InferedTypeProxy
       * 
       */

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
      _indexes = new List<int[]>(NumParameters) {new int[NumParameters]};
      for (int i = 0; i < NumParameters; i++) {
        _indexes[0][i] = 0;
      }
      for (int i = 0; i < NumTests; i++) {
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
    protected object[] CachedValues;
    public System.Type ReturnType { get; protected set; }

    protected ParameterGenerator(ParameterSetGenerator parameterSetGenerator) {
      CachedValues = new object[parameterSetGenerator.NumTests];
    }
    protected ParameterGenerator(int numTests) {
      CachedValues = new object[numTests];
    }

    public object GetNextParameter(int index) {
      return CachedValues[index] ?? (CachedValues[index] = GetNextItem(index));
    }

    protected abstract object GetNextItem(int index);

    public abstract object GetArrayOfSize(int[] indexes);
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

    public override object GetArrayOfSize(int[] indexes) {
      return ArrayFiller.FillArray<BigRational>(indexes, this);
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

    public override object GetArrayOfSize(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
    }
  }

  internal class CharGenerator : ParameterGenerator
  {
    public CharGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      return (char) NextRandomNumberRetriever.GetNextRandomNumber(256);
    }

    public override object GetArrayOfSize(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
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
      int value = NextRandomNumberRetriever.GetNextRandomNumber(5000);
      return new BigInteger(value);
    }

    public override object GetArrayOfSize(int[] indexes) {
      return ArrayFiller.FillArray<BigInteger>(indexes, this);
    }
  }

  class BooleanGenerator : ParameterGenerator
  {
    public BooleanGenerator(ParameterSetGenerator parameterSetGenerator) : base(parameterSetGenerator) {}

    protected override object GetNextItem(int index) {
      return index % 2 == 0;
    }

    public override object GetArrayOfSize(int[] indexes) {
      return ArrayFiller.FillArray<bool>(indexes, this);
    }
  }

  class ArrayGenerator : ParameterGenerator
  {
    private readonly ParameterGenerator _itemGenerator;
    public ArrayGenerator(ParameterSetGenerator parameterSetGenerator, ParameterGenerator itemGenerator): base(parameterSetGenerator) {
      _itemGenerator = itemGenerator;
    }

    protected override object GetNextItem(int index) {
      if (index == 0)
        return _itemGenerator.GetArrayOfSize(new int[0]);
      if (index < 3) // array of size 1 containing first 2 item cases
        return _itemGenerator.GetArrayOfSize(new[] {index-1});
      if (index < 6) //arrays of size 2
        return _itemGenerator.GetArrayOfSize(new[] {index - 3, index - 2});
      //random size of array
      int arraySize = NextRandomNumberRetriever.GetNextRandomNumber(100);
      int[] array = new int[arraySize];
      for (int i = 0; i < arraySize; i++) {
        array[i] = NextRandomNumberRetriever.GetNextRandomNumber(99); //TODO: make this number of tests!!!
      }
      return _itemGenerator.GetArrayOfSize(array);
    }

    public override object GetArrayOfSize(int[] indexes) {
        //TODO need to figure out what to do about nested arrays
        throw new NotImplementedException("Nested empty array generation not yet working due to types");
    }
  }

  class ArrayFiller
  {
    public static T[] FillArray<T>(int[] indexes, ParameterGenerator generator) {
      T[] array = new T[indexes.Length];
      for (int i = 0; i < indexes.Length; i++) {
        array[i] = (T) generator.GetNextParameter(indexes[i]);
      }
      return array;
    }
  }
}

namespace Dafny
{ 
  //Taken from DafnyRuntime.cs - must be in Dafny namespace so Dafny.Rational will mention this
  public struct BigRational
  {
    public static readonly BigRational ZERO = new BigRational(0);

    BigInteger num, den;  // invariant 1 <= den
    public override string ToString()
    {
      return string.Format("({0}.0 / {1}.0)", num, den);
    }
    public BigRational(int n)
    {
      num = new BigInteger(n);
      den = BigInteger.One;
    }
    public BigRational(BigInteger n, BigInteger d)
    {
      // requires 1 <= d
      num = n;
      den = d;
    }
    public BigInteger ToBigInteger()
    {
      if (0 <= num)
      {
        return num / den;
      }
      else
      {
        return (num - den + 1) / den;
      }
    }
    /// <summary>
    /// Returns values such that aa/dd == a and bb/dd == b.
    /// </summary>
    private static void Normalize(BigRational a, BigRational b, out BigInteger aa, out BigInteger bb, out BigInteger dd)
    {
      var gcd = BigInteger.GreatestCommonDivisor(a.den, b.den);
      var xx = a.den / gcd;
      var yy = b.den / gcd;
      // We now have a == a.num / (xx * gcd) and b == b.num / (yy * gcd).
      aa = a.num * yy;
      bb = b.num * xx;
      dd = a.den * yy;
    }
    public int CompareTo(BigRational that)
    {
      // simple things first
      int asign = this.num.Sign;
      int bsign = that.num.Sign;
      if (asign < 0 && 0 <= bsign)
      {
        return -1;
      }
      else if (asign <= 0 && 0 < bsign)
      {
        return -1;
      }
      else if (bsign < 0 && 0 <= asign)
      {
        return 1;
      }
      else if (bsign <= 0 && 0 < asign)
      {
        return 1;
      }
      BigInteger aa, bb, dd;
      Normalize(this, that, out aa, out bb, out dd);
      return aa.CompareTo(bb);
    }
    public override int GetHashCode()
    {
      return num.GetHashCode() + 29 * den.GetHashCode();
    }
    public override bool Equals(object obj)
    {
      if (obj is BigRational)
      {
        return this == (BigRational)obj;
      }
      else
      {
        return false;
      }
    }
    public static bool operator ==(BigRational a, BigRational b)
    {
      return a.CompareTo(b) == 0;
    }
    public static bool operator !=(BigRational a, BigRational b)
    {
      return a.CompareTo(b) != 0;
    }
    public static bool operator >(BigRational a, BigRational b)
    {
      return 0 < a.CompareTo(b);
    }
    public static bool operator >=(BigRational a, BigRational b)
    {
      return 0 <= a.CompareTo(b);
    }
    public static bool operator <(BigRational a, BigRational b)
    {
      return a.CompareTo(b) < 0;
    }
    public static bool operator <=(BigRational a, BigRational b)
    {
      return a.CompareTo(b) <= 0;
    }
    public static BigRational operator +(BigRational a, BigRational b)
    {
      BigInteger aa, bb, dd;
      Normalize(a, b, out aa, out bb, out dd);
      return new BigRational(aa + bb, dd);
    }
    public static BigRational operator -(BigRational a, BigRational b)
    {
      BigInteger aa, bb, dd;
      Normalize(a, b, out aa, out bb, out dd);
      return new BigRational(aa - bb, dd);
    }
    public static BigRational operator -(BigRational a)
    {
      return new BigRational(-a.num, a.den);
    }
    public static BigRational operator *(BigRational a, BigRational b)
    {
      return new BigRational(a.num * b.num, a.den * b.den);
    }
    public static BigRational operator /(BigRational a, BigRational b)
    {
      // Compute the reciprocal of b
      BigRational bReciprocal;
      if (0 < b.num)
      {
        bReciprocal = new BigRational(b.den, b.num);
      }
      else
      {
        // this is the case b.num < 0
        bReciprocal = new BigRational(-b.den, -b.num);
      }
      return a * bReciprocal;
    }
  }
}
