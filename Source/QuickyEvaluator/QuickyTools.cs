using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quicky;
using Dafny;
using Microsoft.Dafny;
using Bpl = Microsoft.Boogie;

namespace QuickyEvaluator
{
  /// <summary>
  /// Used to stop errors being printed to the console during tests.
  /// </summary>
  public class InvisibleErrorReporter : ConsoleErrorReporter
  {
    public override bool Message(MessageSource source, ErrorLevel level, Bpl.IToken tok, string msg) {
      return false;
    }
  }

  public class InvisibleConsolePrinter : Bpl.ConsolePrinter
  {
    public override void ReportBplError(Bpl.IToken tok, string message, bool error, TextWriter tw,
      string category = null) {}

    public new void WriteErrorInformation(Bpl.ErrorInformation errorInfo, TextWriter tw, bool skipExecutionTrace = true) {}
  }

  class QuickyTools
  {
    private static readonly ErrorReporter Reporter = new InvisibleErrorReporter();

    private static string _outputDir;

    private static void Main(string[] args)
    {
      Contract.Requires(args != null);
      QuickyMain.SetupEnvironment();
      Bpl.ExecutionEngine.printer = new Bpl.ConsolePrinter();

      var dafnyPrograms = GetPrograms(args);
      if (dafnyPrograms == null) return;

      Console.WriteLine("1: Test Generator Applicability (how many methods the current generators can support");
      var input = Console.ReadLine();
      var ans = GetInputAsInt(input);

      switch (ans)
      {
        case 1:
          GeneratorApplicability gen = new GeneratorApplicability(dafnyPrograms);
          gen.Test();
          break;
        default:
          Console.WriteLine("Invalid input: " + input);
          break;
      }
      Console.ReadLine();
    }

    private static int GetInputAsInt(string input)
    {
      int ans;
      if (!int.TryParse(input, out ans))
      {
        ans = 0;
      }
      return ans;
    }

    private static List<Program> GetPrograms(string[] args)
    {
//      if (args.Length < 2)
//      {
//        Console.WriteLine("You must enter the location of your z3 executable and your output directory as parameters");
//        return null;
//      }
//
//      _outputDir = args[1];
//
//      var files = args.Skip(2).ToArray();
      var files = args;

      var dafnyPrograms = new List<Program>();
      var fileNames = new List<string>();
      foreach (var file in files)
        fileNames.AddRange(GetFileNames(file));

      foreach (var fileName in fileNames)
      {
        Console.WriteLine("Filename: " + fileName);
        dafnyPrograms.Add(CreateProgramFromFileName(fileName));
      }
      Console.WriteLine("Loaded {0} programs", dafnyPrograms.Count);
      return dafnyPrograms;
    }

    private static Program CreateProgramFromFileName(string fileName)
    {
      var nameStart = fileName.LastIndexOf('\\') + 1;
      var programName = fileName.Substring(nameStart, fileName.Length - nameStart);

      ModuleDecl module = new LiteralModuleDecl(new DefaultModuleDecl(), null);
      var builtIns = new BuiltIns();
      Parser.Parse(fileName, module, builtIns, new Errors(Reporter));

      var program = new Program(programName, module, builtIns, Reporter);
      return program;
    }

    private static List<string> GetFileNames(string arg)
    {
      var fileNames = new List<string>();
      if (arg.EndsWith("\\*") || arg.EndsWith("/*"))
      {
        var newFileNames = Directory.GetFiles(arg.Substring(0, arg.Length - 1));
        fileNames.AddRange(newFileNames.Where(newFileName => newFileName.EndsWith(".dfy")));
      }
      else if (arg.EndsWith(".dfy"))
      {
        fileNames.Add(arg);
      }
      else {
        Console.WriteLine(arg + " not recognised.");
      }
      return fileNames;
    }
  }
}
