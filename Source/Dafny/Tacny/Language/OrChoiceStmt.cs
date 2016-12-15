using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Boogie;
using Tacny;
using Formal = Microsoft.Dafny.Formal;
using Type = Microsoft.Dafny.Type;
using Microsoft.Dafny;

namespace Tacny.Language
{
    class OrChoiceStmt
    {

        public string Signature => "orchoice";
        public bool IsPartial = false;

        // All has to be * if it is non-deterministic - we could change to only one?
        [Pure]
        public static bool IsNonDeterministic(Statement statement)
        {
            Contract.Requires(statement != null);
            if (statement is IfStmt){
                var ifstmt = statement as IfStmt;
                if (ifstmt.Guard == null)
                    return true;
                else
                    return false;
            }
            if (statement is AlternativeStmt){
                var ifstmt = statement as AlternativeStmt;
                foreach (GuardedAlternative a in ifstmt.Alternatives){
                    if (a.Guard != null)
                        return false;
                }
                return true;
            }
            return false;
        }


        public IEnumerable<ProofState> Eval(Statement statement, ProofState state0){
            Contract.Requires(statement != null);
            Contract.Requires(IsNonDeterministic(statement));
            //Contract.Requires(statement is TacnyCasesBlockStmt);

           List<BlockStmt> choices = new List<BlockStmt>();

            if (statement is IfStmt){
                var ifstmt = statement as IfStmt;
                if (ifstmt.Thn != null)
                    choices.Add(ifstmt.Thn as BlockStmt);
                if (ifstmt.Els != null)
                    choices.Add(ifstmt.Els as BlockStmt);
            }
            if (statement is AlternativeStmt){
                var ifstmt = statement as AlternativeStmt;
                foreach (GuardedAlternative a in ifstmt.Alternatives){
                   if (a.Body != null && a.Body.Count != 0)
                        choices.Add(new BlockStmt(a.Body.First().Tok, a.Body.Last().EndTok, a.Body));
                }
            }
            ProofState state = null;
            foreach (BlockStmt choice in choices){
                state = state0.Copy();
                state.AddNewFrame(choice.SubStatements.ToList(), IsPartial);
                yield return state;
            } 
        }
    }
}
