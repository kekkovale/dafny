using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.CSharp;
using Microsoft.Dafny;
using Bpl = Microsoft.Boogie;
using Type = System.Type;
using System.Numerics;


/*

    NEW COMPILE PLAN

    Each method will now have a QuickyChecker object passed in.  This will be called to perform checks etc and track the errors.

    Start of each method:
    - check for preconditions - return if false
    - create a string to show parameter values

*/

namespace Quicky
{
  public static class QuickyMain
  {
    private static readonly ErrorReporter Reporter = new ConsoleErrorReporter();

    public static void Main() {
      SetupEnvironment();
      Program dafnyProgram =
        CreateProgramFromFileName("C:\\Users\\Duncan\\Dissertation\\Quicky\\Test\\quicky\\Test.dfy");
      
      Quicky quicky = new Quicky(dafnyProgram);
      quicky.PerformTesting();

      Console.Read();
    }
    
    public static Program CreateProgramFromFileName(string fileName) {
      var nameStart = fileName.LastIndexOf('\\') + 1;
      var programName = fileName.Substring(nameStart, fileName.Length - nameStart);

      ModuleDecl module = new LiteralModuleDecl(new DefaultModuleDecl(), null);
      var builtIns = new BuiltIns();

      Parser.Parse(fileName, module, builtIns, new Errors(Reporter));

      var program = new Program(programName, module, builtIns, Reporter);
      return program;
    }

    public static void SetupEnvironment() {
      DafnyOptions.Install(new DafnyOptions());
      Bpl.CommandLineOptions.Clo.ApplyDefaultOptions();
      DafnyOptions.O.Z3ExecutablePath = "C:\\Users\\Duncan\\Dissertation\\Quicky\\Binaries\\z3.exe";
      DafnyOptions.O.ApplyDefaultOptions();
      DafnyOptions.O.RunningBoogieFromCommandLine = true;
      DafnyOptions.O.VerifySnapshots = 1;
      DafnyOptions.O.ErrorTrace = 0;
      DafnyOptions.O.ProverKillTime = 15;
      Bpl.ExecutionEngine.printer = new Bpl.ConsolePrinter();
    }
  }

  //This class is referenced from quicky compiled programs to check and react to 
  public class Quicky
  {
    public Dictionary<Method, QuickyException> FoundErrors = new Dictionary<Method, QuickyException>();
    private readonly Program _dafnyProgram;
    private Assembly _assemblyProgram;
    public List<object> Outputs = new List<object>();
    private readonly int _testCases;

    public Quicky(Program dafnyProgram, int testCases = 100) {
      _dafnyProgram = dafnyProgram;
      _testCases = testCases;
      var cSharpProgram = CompileDafnyProgram();
      CompileCsharpProgram(cSharpProgram);
    }

    private TextWriter CompileDafnyProgram() {
      Resolver resolver = new Resolver(_dafnyProgram);
      resolver.ResolveProgram(_dafnyProgram);

      TextWriter tw = new StringWriter();
      Compiler compiler = new Compiler(true);
      compiler.ErrorWriter = Console.Out;
      compiler.Compile(_dafnyProgram, tw);
      using (TextWriter writer = File.CreateText("C:\\Users\\Duncan\\Documents\\Test.cs")) {
        writer.WriteLine(tw.ToString());
      }

      return tw;
    }

    private void CompileCsharpProgram(TextWriter cSharpTw) { 
      var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
      var parameters = new CompilerParameters(GetRequiredReferences()) {GenerateExecutable = false};
      CompilerResults results = csc.CompileAssemblyFromSource(parameters, cSharpTw.ToString());
      results.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText)); //todo throw exception if any errors
      _assemblyProgram = results.CompiledAssembly;
    }

    private static string[] GetRequiredReferences()
    {
      //TODO: This needs to be done in a way so it will work on all systems
      var system = @"System.dll";
      var core = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\System.Core.dll";
      var numericsRef = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NetFramework\v4.5.2\System.Numerics.dll";
      var immutableRef = @"C:\Users\Duncan\Dissertation\Quicky\Source\packages\System.Collections.Immutable.1.3.0\lib\portable-net45+win8+wp8+wpa81\System.Collections.Immutable.dll";
      var quicky = @"C:\Users\Duncan\Dissertation\Quicky\Binaries\Quicky.dll";
      return new[] { system, core, numericsRef, immutableRef, quicky };
    }

    public void PerformTesting() {
      foreach (var module in _dafnyProgram.Modules()) {
        TestModule(module);
      }
    }

    private void TestModule(ModuleDefinition module) {
      foreach (var topLevelDecl in module.TopLevelDecls) {
        if (topLevelDecl is ClassDecl)
          TestClass((ClassDecl) topLevelDecl);
        else if(topLevelDecl is LiteralModuleDecl)
          TestModule((topLevelDecl as LiteralModuleDecl).ModuleDef);
      }
    }

    private void TestClass(ClassDecl classDecl) {
      string className = classDecl.CompileName;
      string typeCall = className + "." + className; //TODO this may need changed for modules etc
      Console.WriteLine(typeCall);
      Type t = _assemblyProgram.GetType(typeCall);
    
      foreach (var member in classDecl.Members) {
        if (member is Method) {
          //TODO multithread here? or for each run? both?
          TestMethod((Method) member, t);
        }
      }
    }

    private void TestMethod(Method method, Type t) {
      Contract.Requires(t != null && method != null);
      string methodName = method.CompileName;
      MethodInfo methodInfo = t.GetMethod(methodName);
      ParameterGenerator parameterGenerator = new ParameterGenerator(this, method);
      
      //todo: multithread here?
      //could do some fancy counter thing to do a few at a time and stop when error is found 
      for (int i = 0; i < _testCases; i++) {
        TestMethodOnce(methodInfo, parameterGenerator);
        if (FoundErrors.ContainsKey(method))
          break;
      }
      Console.WriteLine(parameterGenerator.QuickyChecker.PreconditionFails);
    }

    private void TestMethodOnce(MethodInfo methodInfo, ParameterGenerator parameterGenerator) {
      object[] parameters = parameterGenerator.GetParameterSet();
      methodInfo.Invoke(null, parameters);
//    if outputs is ever needed, they will be in parameters after the actual input parameters
    }
  }
  

  //todo: move error info into here?  can be tracked alongside precondition fails count
  public class QuickyChecker
  {
    private readonly Method _method;
    private readonly Quicky _quicky;
    public int PreconditionFails { get; private set; } = 0;

    public QuickyChecker(Method method, Quicky quicky) {
      _method = method;
      _quicky = quicky;
    }

    public void PreconditionFailed() {
      PreconditionFails++;
    }

    public void CheckAssert(bool outcome, int lineNum, int columnNum, string counterExamples)
    {
      if (outcome)
        //assert holds - do nothing
        return;
      Console.WriteLine("Assert has failed!");
      Bpl.Token tok = new Bpl.Token(lineNum, columnNum);
      var exception = new QuickyException(tok, counterExamples);
      _quicky.FoundErrors.Add(_method, exception);
      //throw exception;
    }
  }

  class ParameterGenerator
  {
    public readonly QuickyChecker QuickyChecker;
    private readonly Method _method;
    private readonly Random _random = new Random();

    public ParameterGenerator(Quicky quicky, Method method) {
      QuickyChecker = new QuickyChecker(method, quicky);
      _method = method;
    }

    public object[] GetParameterSet() {
      List<object> parameters = new List<object>() { QuickyChecker };
      foreach (var param in _method.Ins)
        parameters.Add(GenerateValueOfType(param.SyntacticType));
      foreach (var formal in _method.Outs)
        parameters.Add(null); //nulls needed for outs
      return parameters.ToArray();
    }

    private object GenerateValueOfType(Microsoft.Dafny.Type type) {
      if (type is IntType)
        return GenerateInt();
      throw new Exception("Nothing of type found"); //TODO create new exception type
    }

    //TODO: Something more advanced must be done using the preconditions
    private BigInteger GenerateInt() {
      int value = _random.Next();//TODO: start with smaller numbers and work up somehow so simple examples are given OR integrate fscheck for generation only
      return new BigInteger(value);
    }
  }

  //TODO make exception?
  public class QuickyException : Exception
  {
    public Bpl.IToken Token;
    public string CounterExamples;

    public QuickyException(Bpl.IToken token, string counterExamples) {
      Token = token;
      CounterExamples = counterExamples;
    }
  }

  public class PreconditionFailedException : Exception {}
}
