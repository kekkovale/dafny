﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.Tacny.Expr {
  class EvalExpr {

    /// <summary>
    /// TODO:
    /// simplify tactic expression only, the dafny expression are untouched. This includes
    /// - eval tactic varriables
    /// - eval projection functions, e.g. post
    /// - eval tactic calls
    /// </summary>
    /// <param name="state"></param>
    /// <param name="expr"></param>
    /// <returns></returns>
    public static IEnumerable<Expression> SimpTacExpr(ProofState state, Expression expr){
      Contract.Requires<ArgumentNullException>(state != null, "state");
      Contract.Requires<ArgumentNullException>(expr != null, "expr");

      throw new NotImplementedException();
    }

    /// <summary>
    /// TODO: 
    /// This function should contains two stages:
    /// 1. simplify tactic related terms and expression, by calling SimpTacExpr
    /// 2. ask dafny to evaluate the simplified expression if needed 
    /// </summary>
    /// <param name="state"></param>
    /// <param name="expr"></param>
    /// <param name="ifEvalDafnyExpr"></param>
    /// <returns></returns>
    public static IEnumerable<object> EvalTacnyExpression(ProofState state, Expression expr, bool ifEvalDafnyExpr = true) {
      Contract.Requires<ArgumentNullException>(state != null, "state");
      Contract.Requires<ArgumentNullException>(expr != null, "expr");
      if(expr is NameSegment) {
        var ns = (NameSegment)expr;
        if(state.ContainTacnyVal(ns.Name)) {
          yield return state.GetTacnyVarValue(ns.Name);
        } else {
          yield return ns;
        }
      } else if(expr is ApplySuffix) {
        var aps = (ApplySuffix)expr;
        if(state.IsTacticCall(aps)) {
          /*
          var us = new UpdateStmt(aps.tok, aps.tok, new List<Expression>() { aps.Lhs },
            new List<AssignmentRhs>() { new ExprRhs(aps) });
          foreach(var item in ApplyNestedTactic(state, state.DafnyVars(), us).Select(x => x.GetGeneratedCode())) {
            yield return item;
          }
          */
        } else if(aps.Lhs is ExprDotName) {
          foreach(var item in EvalTacnyExpression(state, aps.Lhs)) {
            if(item is Expression) {
              yield return new ApplySuffix(aps.tok, (Expression)item, aps.Args);
            } else {
              Contract.Assert(false, "Unexpected ExprNotName case");
            }
          }
        } else {
          // get the keyword of this application
          string sig = Util.GetSignature(aps);
          // Try to evaluate as tacny expression
          // using reflection find all classes that extend EAtomic
          var types =
            Assembly.GetAssembly(typeof(EAtomic.EAtomic))
              .GetTypes()
              .Where(t => t.IsSubclassOf(typeof(EAtomic.EAtomic)));
          foreach(var eType in types) {
            var eatomInst = Activator.CreateInstance(eType) as EAtomic.EAtomic;
            if(sig == eatomInst?.Signature) {
              //TODO: validate input countx
              var enumerable = eatomInst?.Generate(aps, state);
              if(enumerable != null)
                foreach(var item in enumerable) {
                  yield return item;
                  yield break;
                }
            }
          }

          // if we reached this point, rewrite the apply suffix
          foreach(var item in EvalTacnyExpression(state, aps.Lhs)) {
            if(!(item is NameSegment)) {
              //TODO: warning
            } else {
              var argList = new List<Expression>();
              foreach(var arg in aps.Args) {
                foreach(var result in EvalTacnyExpression(state, arg)) {
                  if(result is Expression)
                    argList.Add(result as Expression);
                  else
                    argList.Add(Util.VariableToExpression(result as IVariable));
                  break;
                }
              }
              yield return new ApplySuffix(aps.tok, aps.Lhs, argList);
            }
          }
        }
      } else if(expr is ExprDotName) {
        var edn = (ExprDotName)expr;
        var ns = edn.Lhs as NameSegment;
        if(ns != null && state.ContainDafnyVar(ns)) {
          var newLhs = state.GetTacnyVarValue(ns);
          var lhs = newLhs as Expression;
          if(lhs != null)
            yield return new ExprDotName(edn.tok, lhs, edn.SuffixName, edn.OptTypeArguments);
        }
        yield return edn;
      } else if(expr is UnaryOpExpr) {
        var op = (UnaryOpExpr)expr;
        foreach(var result in EvalTacnyExpression(state, op.E)) {
          switch(op.Op) {
            case UnaryOpExpr.Opcode.Cardinality:
              if(!(result is IEnumerable)) {
                var resultExp = result is IVariable
                  ? Util.VariableToExpression(result as IVariable)
                  : result as Expression;
                yield return new UnaryOpExpr(op.tok, op.Op, resultExp);
              } else {
                var enumerator = result as IList;
                if(enumerator != null)
                  yield return new Microsoft.Dafny.LiteralExpr(op.tok, enumerator.Count);
              }
              yield break;
            case UnaryOpExpr.Opcode.Not:
              if(result is Microsoft.Dafny.LiteralExpr) {
                var lit = (Microsoft.Dafny.LiteralExpr)result;
                if(lit.Value is bool) {
                  // inverse the bool value
                  yield return new Microsoft.Dafny.LiteralExpr(op.tok, !(bool)lit.Value);
                } else {
                  Contract.Assert(false);
                  //TODO: error message
                }
              } else {
                var resultExp = result is IVariable ? Util.VariableToExpression(result as IVariable) : result as Expression;
                yield return new UnaryOpExpr(op.tok, op.Op, resultExp);
              }
              yield break;
            default:
              Contract.Assert(false, "Unsupported Unary Operator");
              yield break;
          }
        }
      } else if(expr is DisplayExpression) {
        var dexpr = (DisplayExpression)expr;
        if(dexpr.Elements.Count == 0) {
          yield return dexpr.Copy();
        } else {
          foreach(var item in Interpreter.EvalDisplayExpression(state, dexpr)) {
            yield return item;
          }

        }
      } else if(ifEvalDafnyExpr) {
        var expr0 = expr.Copy();
        var prog = state.GetDafnyProgram();
        new Resolver(prog).ResolveExpression(expr0, new Resolver.ResolveOpts(null, true));
        yield return expr0;
      }
    }

  }
}
