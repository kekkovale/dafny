using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Dafny;
using Microsoft.CSharp;
using Microsoft.Dafny;
using Bpl = Microsoft.Boogie;
using Type = System.Type;

/*
    TODO::

  do NOT use the dafnyRuntime at all - put it into the dafny namespace so it gets read





  Better test gathering?  this would allow multiple errors per function as opposed to just 1 as it is now.  Is this a good thing though?
  
  More paramter types and vary for bigger values with ints?

  Handle infinite loops - some kind of timeout?

  Multithreading.  For methods or all tests?  or break down?

  In extension, only test methods that have been altered?

  Support calcs and preconditions on lemma calls

  use all member types or just Methods?
*/



namespace Quicky
{
  public static class QuickyMain
  {
    private static readonly ErrorReporter Reporter = new ConsoleErrorReporter();
    public static string PrintCompiledCode = null;
    
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

  public class QuickyCompileException : Exception
  {
    public readonly List<CompilerError> Errors;

    public QuickyCompileException(List<CompilerError> errors) : base("Compiling of Quicky program failed:\n" + GetErrorsString(errors)) {
      Contract.Requires(errors != null);
      Errors = errors;
    }

    private static string GetErrorsString(List<CompilerError> errors) {
      Contract.Requires(errors != null);
      string str = "";
      foreach (var compilerError in errors) {
        str += compilerError.ErrorText + "\n";
      }
      return str;
    }
  }

  public class Quicky
  {
    public Dictionary<Method, QuickyError> FoundErrors = new Dictionary<Method, QuickyError>();
    private readonly Program _dafnyProgram;
    private readonly Assembly _assemblyProgram;
    public readonly int TestCases;
    private readonly bool _debug;
    private readonly List<Thread> _threads = new List<Thread>();


    public Quicky(Program dafnyProgram, int testCases = 100, bool debug=false) {
      Contract.Requires(dafnyProgram != null);
      _dafnyProgram = dafnyProgram;
      TestCases = testCases;
      QuickyCompilerHelper qch = new QuickyCompilerHelper(_dafnyProgram);
      _assemblyProgram = qch.AssemblyProgram;
      _debug = debug;
    }

    
    public void PerformTesting() {
      foreach (var module in _dafnyProgram.CompileModules) {
        if (module.CompileName == "_System") continue;
        TestModule(module);
      }
      foreach (Thread thread in _threads)
        thread.Join();
    }

    private void TestModule(ModuleDefinition module) {
      foreach (var topLevelDecl in module.TopLevelDecls) {
        if (!(topLevelDecl is ClassDecl)) continue;
        Thread thread = new Thread(TestClassThread);
        _threads.Add(thread);
        thread.Start(topLevelDecl);
      }
    }

    void TestClassThread(Object o) {
      var classDecl = o as ClassDecl;
      if(classDecl != null)
        TestClass(classDecl);
      else 
        throw new Exception("TestClassThread called without a ClassDecl");
    }

    private void TestClass(ClassDecl classDecl) {
      string typeCall;
      string className = classDecl.CompileName;
      if(classDecl.Module.IsDefaultModule)
        typeCall = "__default" + "." + className;
      else
        typeCall = classDecl.Module.CompileName + "." + className; 

      Type t = _assemblyProgram.GetType(typeCall);
      //TODO handle non-static methods?
      foreach (var member in classDecl.Members)
        if (member is Method && member.IsStatic)
          TestMethod((Method) member, t);
    }

    private void TestMethod(Method method, Type t) {
      Contract.Requires(t != null && method != null);
      if (method.Ins.Count < 1) return; //no paramaters, cannot really test.  May do later to show what's wrong?
      MethodInfo methodInfo = t.GetMethod(method.CompileName);
      ParameterSetGenerator parameterSetGenerator;
      try {
        parameterSetGenerator = new ParameterSetGenerator(this, method);
      }
      catch (UnidentifiedDafnyTypeException e) {
        Console.WriteLine("Could not generate parameters for method " + method.Name + " of type " + e.Type);
        return;
      }
      catch (Exception) {
        if (_debug) throw; //some other type of error has occured that shouldn't.  If _debug is on, it's thrown so programmer can see error.
        return;
      }

      //todo: multithread more here?
      for (int i = 0; i < TestCases; i++) {
        TestMethodOnce(methodInfo, parameterSetGenerator);
        if (FoundErrors.ContainsKey(method))
          break;
      }
    }

    private void TestMethodOnce(MethodInfo methodInfo, ParameterSetGenerator parameterSetGenerator) {
      object[] parameters = parameterSetGenerator.GetNextParameterSet();
      methodInfo.Invoke(null, parameters);
//    if outputs is ever needed, they will be in parameters after the actual input parameters (they are passed as outs)
    }
  }

  class QuickyCompilerHelper
  {
    private readonly Program _dafnyProgram;
    public Assembly AssemblyProgram;

    public QuickyCompilerHelper(Program dafnyProgram) {
      _dafnyProgram = dafnyProgram;
      var cSharpProgram = CompileDafnyProgram();
      AssemblyProgram = CompileCsharpProgram(cSharpProgram);

    }

    private TextWriter CompileDafnyProgram()
    {
      TextWriter tw = new StringWriter();
      Compiler compiler = new Compiler(true) { ErrorWriter = Console.Out };
      compiler.Compile(_dafnyProgram, tw);
      if (QuickyMain.PrintCompiledCode != null) {
        using (TextWriter writer = File.CreateText(QuickyMain.PrintCompiledCode)) {
          writer.WriteLine(tw.ToString());
        }
      }
      return tw;
    }

    private Assembly CompileCsharpProgram(TextWriter cSharpTw)
    {
      var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });
      var parameters = new CompilerParameters(GetRequiredReferences()) { GenerateExecutable = false };
      CompilerResults results = csc.CompileAssemblyFromSource(parameters, cSharpTw.ToString());
      var errors = results.Errors.Cast<CompilerError>().ToList();
      if (errors.Count > 0) throw new QuickyCompileException(errors);
      return results.CompiledAssembly;
    }

    private static string[] GetRequiredReferences()
    {
      var system = @"System.dll";
      var core = typeof(Enumerable).Assembly.Location;
      var numerics = typeof(BigInteger).Assembly.Location;
      var dafnyRuntime = typeof(BigRational).Assembly.Location;
      var quicky = Assembly.GetExecutingAssembly().CodeBase.Substring(8);
      return new[] { system, core, numerics, dafnyRuntime, quicky };
    }

  }
}
