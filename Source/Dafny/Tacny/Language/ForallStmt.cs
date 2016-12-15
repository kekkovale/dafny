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
    class ForallStmt
    {
        /*
         *  TODO: generalise: this could also be a meta-level variable or tactic application
         */
        [Pure]
        public static bool IsForallShape(Expression e){
            Contract.Requires(e != null);

            if (e is Microsoft.Dafny.ForallExpr){
                var fall = e as Microsoft.Dafny.ForallExpr;
                if (fall.LogicalBody() is BinaryExpr){
                    var body = fall.LogicalBody() as BinaryExpr;
                    if (body?.Op == BinaryExpr.Opcode.Imp){
                        return true;
                    }else{
                        return false;
                    }    
                }
            }
            return false;
        }

        public static IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
            Contract.Requires(statement != null);
            Contract.Requires(statement is TacnyForallStmt);

            var stmt = statement as TacnyForallStmt;
            var e = stmt.Spec;
            var attr = stmt.Attributes;
            
            Contract.Assert(e != null && IsForallShape(e));
            
            return null;
        }
    }
}
