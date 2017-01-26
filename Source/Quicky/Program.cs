using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.Dafny;
using Bpl = Microsoft.Boogie;
using Type = System.Type;

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
    public static bool PrintCompiledCode;

    public static void Main() {
      SetupEnvironment();
      Program dafnyProgram =
        CreateProgramFromFileName(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory) + @"\Test\quicky\Test01.dfy");
      
      Quicky quicky = new Quicky(dafnyProgram);
      quicky.PerformTesting();

      Console.Read();
    }
    
    public static Program CreateProgramFromFileName(string fileName) {
      Contract.Requires(fileName != null);
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
      DafnyOptions.O.Z3ExecutablePath = AppDomain.CurrentDomain.BaseDirectory + "\\z3.exe";
      DafnyOptions.O.ApplyDefaultOptions();
      DafnyOptions.O.RunningBoogieFromCommandLine = true;
      DafnyOptions.O.VerifySnapshots = 1;
      DafnyOptions.O.ErrorTrace = 0;
      DafnyOptions.O.ProverKillTime = 15;
      Bpl.ExecutionEngine.printer = new Bpl.ConsolePrinter();
    }

    public static string BinariesDirectory() {
      var baseDir = AppDomain.CurrentDomain.BaseDirectory;
      return Path.GetFileName(baseDir) == "Binaries" ? baseDir : Directory.GetParent(baseDir).ToString();
    }
  }
  
  public class Quicky
  {
    public Dictionary<Method, QuickyError> FoundErrors = new Dictionary<Method, QuickyError>();
    private readonly Program _dafnyProgram;
    private readonly Assembly _assemblyProgram;
    public List<object> Outputs = new List<object>();
    private readonly int _testCases;

    public Quicky(Program dafnyProgram, int testCases = 100) {
      Contract.Requires(dafnyProgram != null);
      _dafnyProgram = dafnyProgram;
      _testCases = testCases;
      var cSharpProgram = CompileDafnyProgram();
      _assemblyProgram = CompileCsharpProgram(cSharpProgram);
    }

    private TextWriter CompileDafnyProgram() {
      Resolver resolver = new Resolver(_dafnyProgram);
      resolver.ResolveProgram(_dafnyProgram);

      TextWriter tw = new StringWriter();
      Compiler compiler = new Compiler(true) {ErrorWriter = Console.Out};
      compiler.Compile(_dafnyProgram, tw);
      if (QuickyMain.PrintCompiledCode) {
        using (TextWriter writer = File.CreateText("C:\\Users\\Duncan\\Documents\\Test.cs")) {
          writer.WriteLine(tw.ToString());
        }
      }
      return tw;
    }

    private Assembly CompileCsharpProgram(TextWriter cSharpTw) { 
      var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
      var parameters = new CompilerParameters(GetRequiredReferences()) {GenerateExecutable = false};
      CompilerResults results = csc.CompileAssemblyFromSource(parameters, cSharpTw.ToString());
      results.Errors.Cast<CompilerError>().ToList().ForEach(error => System.Diagnostics.Debug.WriteLine(error.ErrorText)); //todo throw exception if any errors
      return results.CompiledAssembly;
    }

    private static string[] GetRequiredReferences()
    {
      //TODO: This needs to be done in a way so it will work on all systems
      //(e.g. on 32 bit systems it will be in /Program Files/ instead of /Program Files (x86)/, Assemblies may be installed elsewhere?)
      var system = @"System.dll";
      var core = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\System.Core.dll";
      var numericsRef = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NetFramework\v4.5.2\System.Numerics.dll";
      var quicky = AppDomain.CurrentDomain.BaseDirectory + @"\Quicky.dll";
      return new[] { system, core, numericsRef, /*immutableRef,*/ quicky };
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
      //Console.WriteLine(typeCall);
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
      ParameterSetGenerator parameterSetGenerator;
      try {
        parameterSetGenerator = new ParameterSetGenerator(this, method);
      }
      catch (UnidentifiedDafnyTypeException e) {
        Console.WriteLine("Could not generate parameters for method " + method.Name + " of type " + e.Type);
        return;
      }

      //todo: multithread here?
      //could do some fancy counter thing to do a few at a time and stop when error is found 
      for (int i = 0; i < _testCases; i++) {
        TestMethodOnce(methodInfo, parameterSetGenerator);
        if (FoundErrors.ContainsKey(method))
          break;
      }
      //Console.WriteLine(parameterGenerator.QuickyChecker.PreconditionFails);
    }

    private void TestMethodOnce(MethodInfo methodInfo, ParameterSetGenerator parameterSetGenerator) {
      object[] parameters = parameterSetGenerator.GetNextParameterSet();
      methodInfo.Invoke(null, parameters);
//    if outputs is ever needed, they will be in parameters after the actual input parameters
    }
  }



  


  class IndividualParamaterGenerator
  {
    public Type Type { get; set; }

    public IndividualParamaterGenerator(Type type) {
      Type = type;
    }
  }
  
  public class QuickyError
  {
    public enum ErrorType
    {
      Postcondition,
      Assert,
      InvariantEntry,
      InvariantEnd
    }

    //Error messages to be displayed for certain types of failures
    private static Dictionary<QuickyError.ErrorType, string> _errorMessages = new Dictionary<QuickyError.ErrorType, string>() {
      {QuickyError.ErrorType.Postcondition, "Postcondition failed"},
      {QuickyError.ErrorType.Assert, "Assert failed"},
      {QuickyError.ErrorType.InvariantEntry, "Invariant failed on entry"},
      {QuickyError.ErrorType.InvariantEnd, "Invariant failed at the end of a loop iteration"}
    };

    public ErrorType TypeOfError;
    public Bpl.IToken Token;
    public string CounterExamples;

    public string Message => _errorMessages[TypeOfError] + " with parameters: " + CounterExamples;

    public QuickyError(Bpl.IToken token, string counterExamples, ErrorType errorType) {
      Token = token;
      CounterExamples = counterExamples;
      TypeOfError = errorType;
    }
  }
}
