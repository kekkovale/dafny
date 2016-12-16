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
using Microsoft.Dafny.Tacny;


namespace Tacny.Language
{
    /*
     * TODO: instead of storing all the information in the frame. Why doesn't these just inherit some atomicLang interface
     * and we push that to the frame?
     * Same will happen for while/if/match etc
     * Domain specific stuff can then be stored in the object
     * 
     */
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

        public static IEnumerable<ProofState> EvalNext(Statement statement, ProofState state0) {

            yield break;
        }

        public static IEnumerable<ProofState> EvalInit(Statement statement, ProofState state0){
            Contract.Requires(statement != null);
            Contract.Requires(statement is TacnyForallStmt);

            IToken newtok = TokenGenerator.NextToken();

            var stmt = statement as TacnyForallStmt;
            var e = stmt.Spec;
            var attr = stmt.Attributes;
            var nms = new List<String>();
            if (attr.Name == "") {
                var nms_args = attr.Args; // need to extract name SHould we fail otherwise?
                foreach (Expression ee in nms_args) {
                    if (ee is StringLiteralExpr) {
                        var st = ee as StringLiteralExpr;
                        nms.Add(st.AsStringLiteral());
                    }
                }
            }

            Contract.Assert(e != null && IsForallShape(e));

            // var body = new Microsoft.Dafny.ForallStmt();
            //q = new ForallExpr(x, bvars, range, body, attrs);
            //s = new ForallStmt(x, tok, bvars, attrs, range, ens, block);

            var state = state0.Copy();

            yield return state;
        }
    }
}
