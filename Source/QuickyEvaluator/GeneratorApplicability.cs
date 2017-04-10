using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dafny;
using Quicky;
using Type = Microsoft.Dafny.Type;

namespace QuickyEvaluator
{
  class GeneratorApplicability
  {
    private readonly List<Program> Programs;

    public int SuccessfulMethods = 0;
    public int SuccessfulParams = 0;

    public int FailedMethods = 0;
    public int FailedParameters = 0;

    public int OtherGeneratorFails = 0;

    public int OtherMiscFails = 0;

    public Dictionary<string, int> FailedMethodTypes = new Dictionary<string, int>();
    

    public GeneratorApplicability(List<Program> programs) {
      Programs = programs;
    }

    private void AddDictionary(Dictionary<string, int> newFails) {
      foreach (var type in newFails.Keys) {
        if (FailedMethodTypes.ContainsKey(type))
          FailedMethodTypes[type] += newFails[type];
        else
          FailedMethodTypes.Add(type, newFails[type]);
      }
    }

    public void Test() {
      Console.WriteLine("\nTesting for {0} Programs", Programs.Count);
      int i = 0;
      foreach (var program in Programs) {
        try
        {
          Resolver resolver = new Resolver(program);
          resolver.ResolveProgram(program);
          Console.WriteLine("Testing {0} | {1}/{2}",program.Name, ++i, Programs.Count);
        
          Tester quicky = new Tester(program);

          quicky.PerformTesting();

          SuccessfulMethods += quicky.SuccessfulMethods;
          SuccessfulParams += quicky.SuccessfulParameters;

          FailedMethods += quicky.FailedMethods;
          FailedParameters += quicky.FailedParameters;
          AddDictionary(quicky.FailedMethodTypes);

          OtherGeneratorFails += quicky.OtherFails;
          Console.WriteLine("Passed with {0} successful methods and {1} unsuccessful methods",quicky.SuccessfulMethods, quicky.FailedMethods);
        }
        catch (Exception e) {
          OtherMiscFails++;
          Console.WriteLine("Failure Occured: {0}", e.Message);
        }
      }


      Console.WriteLine("\n\n--ALL TESTS COMPLETE:--\n");
      Console.WriteLine("Successful Methods: {0}\nSuccessful Parameters: {1}\n\n", SuccessfulMethods, SuccessfulParams);

      Console.WriteLine("Failed Methods: {0}\nFailed Paramters: {1}\n",FailedMethods, FailedParameters);

      Console.WriteLine("Fails Breakdown:");
      foreach (var type in FailedMethodTypes.Keys) {
        Console.WriteLine("  {0}: {1}",type, FailedMethodTypes[type]);
      }

      Console.WriteLine("\n\nMethods with unexpected Generator failures: {0}", OtherGeneratorFails);
      Console.WriteLine("Number of methods that did not reach generation stage: {0}", OtherMiscFails);
    }


  }




  class Tester
  {
    private Program _program;


    public int SuccessfulMethods = 0;
    public int SuccessfulParameters = 0;

    public int FailedMethods = 0;
    public Dictionary<string, int> FailedMethodTypes = new Dictionary<string, int>();
    public int FailedParameters = 0;

    public int OtherFails = 0;

    public Tester(Program program) {
      this._program = program;
    }

    public void PerformTesting()
    {
      foreach (var module in _program.CompileModules)
      {
        if (module.CompileName == "_System") continue;
        TestModule(module);
      }

    }

    private void TestModule(ModuleDefinition module)
    {
      foreach (var topLevelDecl in module.TopLevelDecls)
      {
        if (!(topLevelDecl is ClassDecl)) continue;
        TestClass(topLevelDecl as ClassDecl);

      }

    }

    private void TestClass(ClassDecl classDecl)
    {
      foreach (var member in classDecl.Members)
        if (member is Method && member.IsStatic && !member.IsGhost)
        {
          TestMethod(member as Method);
        }
    }

    protected virtual void TestMethod(Method method)
    {
      Contract.Requires(method != null);
      if (method.Ins.Count < 1) return; //no paramaters, cannot really test.  May do later to show what's wrong?
      ParameterSetGenerator parameterSetGenerator;
      try
      {
        parameterSetGenerator = new ParameterSetGenerator(null, method);
      }
      catch (UnidentifiedDafnyTypeException e)
      {
        FailedMethods++;
        FailedParameters += e.Types.Count;
        foreach (var stype in e.Types) {
          var type = GetTypeName(stype);
          if (FailedMethodTypes.ContainsKey(type))
            FailedMethodTypes[type]++;
          else
            FailedMethodTypes.Add(type, 1);
        }
        SuccessfulParameters += method.Ins.Count - e.Types.Count;  //The parameters in the method that still passed.
        return;
      }
      catch (Exception)
      {
        OtherFails++;
        return;
      }
      SuccessfulMethods++;
      SuccessfulParameters += method.Ins.Count;
    }

    private static string GetTypeName(Type type) {
      if (type.AsDatatype != null)
        return "datatype";
      if (type.AsMapType != null)
        return "maptype";
      if (type.AsArrowType != null)
        return "arrowType";
      if (type.AsCoDatatype != null)
        return "coDataType";
      if (type.AsIndDatatype != null)
        return "indDataType";
      if (type.AsInternalTypeSynonym != null)
        return "InternalTypeSynonym";
      if (type.AsMultiSetType != null)
        return "MultiSet";
      if (type.AsNewtype != null)
        return "Newtype";
      if (type.AsCollectionType != null)
        return "Collection Type";
      if (type.AsRedirectingType != null)
        return "RedirectingType";
      if (type.AsTypeParameter != null)
        return "TypeParameter";

      if (type is UserDefinedType) {
        var uType = type as UserDefinedType;
        if (uType.ResolvedClass != null)
          return "Class";
      }
//      if (type.ResolvedClass != null)

      return type.ToString();
    }
  }
// 
//  class SubQuicky : Quicky.Quicky
//  {
//    public SubQuicky(Program dafnyProgram, int testCases = 100, bool debug = false) : base(dafnyProgram, testCases, debug) {}
//
//    public int SuccessfulMethods = 0;
//    public int SuccessfulParameters = 0;
//
//    public int FailedMethods = 0;
//    public Dictionary<Type, int> FailedMethodTypes = new Dictionary<Type, int>();
//    public int FailedParameters = 0;
//
//    public int OtherFails = 0; // some unexpevted error has occured during parameter generation
//
//    protected override void TestMethod(Method method, System.Type t) {
//      Contract.Requires(t != null && method != null);
//      if (method.Ins.Count < 1) return; //no paramaters, cannot really test.  May do later to show what's wrong?
//
////      MethodInfo methodInfo = t.GetMethod(method.CompileName);
//      ParameterSetGenerator parameterSetGenerator;
//      try {
//        parameterSetGenerator = new ParameterSetGenerator(this, method);
//      }
//      catch (UnidentifiedDafnyTypeException e) { 
//        FailedMethods++;
//        FailedParameters += e.Types.Count;
//        foreach (var type in e.Types) {
//          if (FailedMethodTypes.ContainsKey(type))
//            FailedMethodTypes[type]++;
//          else 
//            FailedMethodTypes.Add(type, 1);
//        }
//        SuccessfulParameters += method.Ins.Count - e.Types.Count;  //The parameters in the method that still passed.
//        return;
//      }
//      catch (Exception) {
//        OtherFails++;
//        return;
//      }
//      SuccessfulMethods++;
//      SuccessfulParameters += method.Ins.Count;
//    }
//
//  }
}
