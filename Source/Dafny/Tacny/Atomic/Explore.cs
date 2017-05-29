using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Dafny.Tacny.Expr;

namespace Microsoft.Dafny.Tacny.Atomic
{

  class Explore : Atomic
  {
    public override string Signature => "explore";

    public override int ArgsCount => 2;

    public override IEnumerable<ProofState> Generate(Statement statement, ProofState state) {
      List<List<IVariable>> args = new List<List<IVariable>>();
      List<IVariable> mdIns = new List<IVariable>();
      List<Expression> callArguments;
      bool branchGenerated = false;
      state.NeedVerify = true;
      
      InitArgs(state, statement, out callArguments);

      /**********************
       * init lemmas
       **********************/
      //check the number of arguments
      if (callArguments == null || callArguments.Count != 2) {
        state.ReportTacticError(statement.Tok, " The number of arguments for explore is not correct, expect 2.");
        yield break;
      }
      // get the first arg: a sequence of lemmas
      var members0 = EvalExpr.EvalTacticExpression(state, callArguments[0]) as SeqDisplayExpr;
      if (members0 == null) {
        state.ReportTacticError(statement.Tok, Printer.ExprToString(callArguments[0]) + " is not a sequence.");
        yield break;
      }
      List<MemberDecl> members = new List<MemberDecl>();

      foreach (var mem in members0.Elements) {
        if (!(mem is TacticLiteralExpr)) {
          state.ReportTacticError(statement.Tok,
            "In " + Printer.ExprToString(callArguments[0]) + 
            Printer.ExprToString(mem) + " is not a lemma.");
          yield break;
        }
        var key = (string) (mem as TacticLiteralExpr).Value;
        if (state.Members.ContainsKey(key))
          members.Add(state.Members[key]);
        else {
          state.ReportTacticError(statement.Tok,  
            "In " + Printer.ExprToString(callArguments[0]) + ", " +  
            key + " is not a lemma.");
          yield break;
        }
      }
      if (members.Count == 0) {
        branchGenerated = true;
        yield return state;
        yield break;
      }

      foreach (var member in members) {
        mdIns.Clear();
        args.Clear();

        var md = (MemberDecl) member;

        // take the membed decl parameters
        var method = md as Method;
        if(method != null)
          mdIns.AddRange(method.Ins);
        else if(md is Function)
          mdIns.AddRange(((Function)md).Formals);
        else {
          state.ReportTacticError(statement.Tok, 
            Printer.ExprToString(callArguments[0]) + " is neither a Method or a Function");
          yield break;
        }

        /**********************
         * init args for lemmas
         **********************/
        var ovars = EvalExpr.EvalTacticExpression(state, callArguments[1]) as SeqDisplayExpr;
        if(ovars == null) {
          state.ReportTacticError(statement.Tok, Printer.ExprToString(callArguments[1]) + " is not a sequence.");
          yield break;
        }

        List<IVariable> vars = new List<IVariable>();

        foreach (var var in ovars.Elements) {
          string key;
          if(var is TacticLiteralExpr)
            key = (string) (var as TacticLiteralExpr).Value;
          else if (var is NameSegment) {
            key = (var as NameSegment).Name;      
          } else {
            state.ReportTacticError(statement.Tok, 
              "In " + Printer.ExprToString(callArguments[1]) + ", " + 
              Printer.ExprToString(var) + " is not a dafny variable.");
            yield break;
          }

          if (state.GetAllDafnyVars().ContainsKey(key))
            vars.Add(state.GetAllDafnyVars()[key].Variable);
          else {
            state.ReportTacticError(statement.Tok,
              "In " + Printer.ExprToString(callArguments[1]) + ", " + 
              key + " is not in scope.");
            yield break;
          }
        }

        //for the case of no args, just add an empty list
        if (mdIns.Count == 0) {
          args.Add(new List<IVariable>());
        }
        //if any of the arguements is not valid, set it to false.
        bool flag = true;
        for (int i = 0; i < mdIns.Count; i++) {
          var item = mdIns[i];
          args.Add(new List<IVariable>());
          foreach (var arg in vars) {
            // get variable type
            Type type = state.GetDafnyVarType(arg.Name);
            if (type != null) {
              if (type is UserDefinedType && item.Type is UserDefinedType) {
                var udt1 = type as UserDefinedType;
                var udt2 = (UserDefinedType) item.Type;
                if (udt1.Name == udt2.Name)
                  args[i].Add(arg);
              } else {
                // if variable type and current argument types match, or the type is yet to be inferred
                if (item.Type.ToString() == type.ToString() || type is InferredTypeProxy)
                  args[i].Add(arg);
              }
            } else
              args[i].Add(arg);
          }
          /**
           * if no type correct variables have been added we can return
           * because we won't be able to generate valid calls
           */
          if (args[i].Count == 0) {
            flag = false;
          }
        }
        // permute lemma call for the current lemma
        if (flag) {
          foreach (var result in PermuteArguments(args, 0, new List<NameSegment>())) {
            // create new fresh list of items to remove multiple references to the same object
            List<Expression> newList = result.Cast<Expression>().ToList().Copy();
            ApplySuffix aps = new ApplySuffix(callArguments[0].tok, 
              new NameSegment(callArguments[0].tok, md.Name, null),
              newList);

              var newState = state.Copy();
              UpdateStmt us = new UpdateStmt(aps.tok, aps.tok, new List<Expression>(),
                new List<AssignmentRhs> {new ExprRhs(aps)});
              newState.AddStatement(us);
              branchGenerated = true;
              yield return newState;
          }
        }
      }
      // for the case when no lemma call is generated.
      if (!branchGenerated)
        yield return state;
    }


    private static IEnumerable<List<NameSegment>> PermuteArguments(List<List<IVariable>> args, int depth, List<NameSegment> current) {
      if (args.Count == 0)
        yield break;
      if (depth == args.Count) {
        yield return current;
        yield break;
      }
      if (args[depth].Count == 0) {
        yield return new List<NameSegment>();
        yield break;
      }
      for (int i = 0; i < args[depth].Count; ++i) {
        List<NameSegment> tmp = new List<NameSegment>();
        tmp.AddRange(current);
        IVariable iv = args[depth][i];
        NameSegment ns = new NameSegment(iv.Tok, iv.Name, null);
        tmp.Add(ns);
        foreach (var item in PermuteArguments(args, depth + 1, tmp))
          yield return item;
      }
    }

  }
}
