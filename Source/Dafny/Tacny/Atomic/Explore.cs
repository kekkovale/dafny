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
      IVariable lv;
      InitArgs(state, statement, out lv, out callArguments);

      state.NeedVerify = true;

      //TODO: implement this properly
      //var members = state.GetLocalValue(callArguments[0] as NameSegment) as IEnumerable<MemberDecl>;
      //evaluate the argument (methods/lemma)
      var members0 = SimpExpr.UnfoldTacticProjection(state, callArguments[0]) as SeqDisplayExpr;
      List<MemberDecl> members = new List<MemberDecl>();

      foreach (var mem in members0.Elements) {
        var key = (string) (mem as TacticLiteralExpr).Value;
        if (state.Members.ContainsKey(key))
          members.Add(state.Members[key]);
      }
      if (members.Count == 0) {
        yield break;
      }

      foreach (var member in members) {
        mdIns.Clear();
        args.Clear();

        var md = (MemberDecl) member;

        // take the membed decl parameters
        var method = md as Method;
        if (method != null)
          mdIns.AddRange(method.Ins);
        else if (md is Function)
          mdIns.AddRange(((Function) md).Formals);
        else
          Contract.Assert(false, "In Explore Atomic call," + callArguments[0] + "is neither a Method or a Function");

        //evaluate the arguemnts for the lemma to be called
        var ovars = SimpExpr.UnfoldTacticProjection(state, callArguments[1]) as SeqDisplayExpr;
        List<IVariable> vars = new List<IVariable>();

        foreach (var var in ovars.Elements) {
          var key = (string)(var as TacticLiteralExpr).Value;
          if (state.GetAllDafnyVars().ContainsKey(key))
            vars.Add(state.GetAllDafnyVars()[key].Variable);
        }

        //for the case when no args, just add an empty list
        if (mdIns.Count == 0) {
          args.Add(new List<IVariable>());
        }
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
           * if no type correct variables have been added we can safely return
           * because we won't be able to generate valid calls
           */
          if (args[i].Count == 0) {
            Debug.WriteLine("No type matching variables were found");
            yield break;
          }
        }

        var count = 0;
        foreach (var result in PermuteArguments(args, 0, new List<NameSegment>())) {
          // create new fresh list of items to remove multiple references to the same object
          List<Expression> newList = result.Cast<Expression>().ToList().Copy();
          ApplySuffix aps = new ApplySuffix(callArguments[0].tok, new NameSegment(callArguments[0].tok, md.Name, null),
            newList);
          if (lv != null) {
            var newState = state.Copy();
            newState.AddTacnyVar(lv, aps);
            yield return newState;
          } else {
            var newState = state.Copy();
            UpdateStmt us = new UpdateStmt(aps.tok, aps.tok, new List<Expression>(),
              new List<AssignmentRhs> { new ExprRhs(aps) });
            //Printer p = new Printer(Console.Out);
            //p.PrintStatement(us,0);
            newState.AddStatement(us);
            count++;
            yield return newState;
          }
        }

      }
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
