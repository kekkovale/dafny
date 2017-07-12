using Microsoft.Dafny.Tacny;
using System;
using Microsoft.Dafny;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;

namespace Microsoft.Dafny.refactoring
{
    
    public class SimpleCloner
    {
        
        
        public static Program CloneProgram(Program program)
        {
            
            var moduleDecl = new LiteralModuleDecl(program.DefaultModuleDef.Copy(), null);
            return new Program(program.FullName, moduleDecl, program.BuiltIns, new InvisibleErrorReporter());
        }

        
    }


    class SomeRefactoring : Cloner
    {
        
        private static MemberDecl FindMemberInFunction(int pos, IList<MemberDecl> memberDecls, Program program)
        {
            
            MemberDecl foundMember = null;
            List<TopLevelDecl> decls = program.DefaultModuleDef.TopLevelDecls;

            foreach(var TopLevelDecls in decls)
            {
               
            }

            foreach (var memberDecl in memberDecls)
            {
                if (memberDecl.tok.pos > pos) continue;
                if (foundMember == null)
                    foundMember = memberDecl;
                else if (memberDecl.tok.pos > foundMember.tok.pos)
                    foundMember = memberDecl;
            }
            return foundMember;
        }

        public Program renameMethod(Program program)
        {
            //MaybeFreeExpression E = new MaybeFreeExpression();
            List<MemberDecl> newMembers = new List<MemberDecl>();

            var tld = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;


            List<TopLevelDecl> list = program.DefaultModuleDef.TopLevelDecls;
            foreach (TopLevelDecl decl in list)
            { 
                if (decl is ClassDecl){
                    ClassDecl cl = (ClassDecl)decl;
                    String newName = "Prova";
                    foreach (MemberDecl member in cl.Members)
                    {
                        
                        if (member.Name == "Abs" && member is Method)
                        {
                            
                            int index = tld.Members.IndexOf(member);
                                                      
                            var clonedMethod = RenameMethod(member, newName);

                            tld.Members.RemoveAt(index);
                            newMembers.Add(clonedMethod);
                            tld.Members.InsertRange(index, newMembers);

                            Console.WriteLine("CLONED NEWNAME {0}", clonedMethod.Name);
                            break;
                        }

                    }
                }
               
            }
            

            return program;
        }

        public MemberDecl RenameMethod(MemberDecl member, String newName)
        {
            if (member is Method)
            {
                var m = (Method)member;
                return CloneMethod(m, newName);
            }
            else
                return null;
        }

        public Method CloneMethod(Method m, String newName)
        {
            Contract.Requires(m != null);

            var tps = m.TypeArgs.ConvertAll(CloneTypeParam);
            var ins = m.Ins.ConvertAll(CloneFormal);
            var req = m.Req.ConvertAll(CloneMayBeFreeExpr);
            var mod = CloneSpecFrameExpr(m.Mod);
            var decreases = CloneSpecExpr(m.Decreases);

            var ens = m.Ens.ConvertAll(CloneMayBeFreeExpr);

            BlockStmt body = CloneMethodBody(m);

            if (m is Constructor)
            {
                return new Constructor(Tok(m.tok), newName, tps, ins,
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
            else if (m is InductiveLemma)
            {
                return new InductiveLemma(Tok(m.tok), newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
            else if (m is CoLemma)
            {
                return new CoLemma(Tok(m.tok), newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
            else if (m is Lemma)
            {
                return new Lemma(Tok(m.tok), newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
            else if (m is TwoStateLemma)
            {
                var two = (TwoStateLemma)m;
                return new TwoStateLemma(Tok(m.tok), newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
            else if (m is Tactic)
            {
                return new Tactic(Tok(m.tok), newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                    req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null);
            }
            else
            {
                return new Method(Tok(m.tok), newName, m.HasStaticKeyword, m.IsGhost, tps, ins, m.Outs.ConvertAll(CloneFormal),
                  req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
            }
        }


        public Program renameVariable(Program program)
        {
            //MaybeFreeExpression E = new MaybeFreeExpression();
            List<MemberDecl> newMembers = new List<MemberDecl>();
            
            var tld = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
            
            //member in Inputs
            foreach (MemberDecl member in tld.Members)
            {
                
                if (member is Method)
                {
                    Method m = member as Method;
                    
                    
                    foreach (Formal fs in m.Ins)
                    {
                        if (fs.DisplayName == "x")
                        {
                            var newFormal = CloneFormal(fs, "z");
                            m.Ins.Remove(fs);
                            m.Ins.Add(newFormal);
                            break;
                        }
                    }
                    
                }
                
            }
 
            return program;
        }

        public Program renameInBodyVariable(Program program)
        {
            //MaybeFreeExpression E = new MaybeFreeExpression();
            List<MemberDecl> newMembers = new List<MemberDecl>();

            var tld = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
                       
            foreach (MemberDecl member in tld.Members)
            {

                if (member is Method)
                {
                    Method m = member as Method;

                    List<Statement> bodyStmts = m.Body.Body;

                    foreach (Statement s in bodyStmts)
                    {
                        if(s is IfStmt)
                        {
                            IfStmt ifStmt = s as IfStmt;
                            if(ifStmt.Guard is BinaryExpr)
                            {
                                BinaryExpr guard = ifStmt.Guard as BinaryExpr;
                                if(guard.E0 is NameSegment)
                                {
                                    
                                    int index = bodyStmts.IndexOf(s);
                                    Expression newExpr = CloneExpr(guard, "z");
                                    var r = new IfStmt(Tok(ifStmt.Tok), Tok(ifStmt.EndTok), ifStmt.IsExistentialGuard, CloneExpr(newExpr), CloneBlockStmt(ifStmt.Thn), CloneStmt(ifStmt.Els));
                                    //Statement newStmt = CloneStmt(s, newExpr);
                                    bodyStmts.RemoveAt(index);
                                    bodyStmts.Add(r);


                                    Console.WriteLine("GUARD {0}");
                                    break;
                                }                      
                                    
                            }
                                
                            
                        }
                        

                    }

                }

            }

            return program;
        }

        public Expression CloneExpr(Expression expr, String newName)
        {
            if (expr == null)
            {
                return null;
            }
            else if (expr is LiteralExpr)
            {
                var e = (LiteralExpr)expr;
                if (e is StaticReceiverExpr)
                {
                    var ee = (StaticReceiverExpr)e;
                    return new StaticReceiverExpr(e.tok, CloneType(ee.UnresolvedType), ee.IsImplicit);
                }
                else if (e.Value == null)
                {
                    return new LiteralExpr(Tok(e.tok));
                }
                else if (e.Value is bool)
                {
                    return new LiteralExpr(Tok(e.tok), (bool)e.Value);
                }
                else if (e is CharLiteralExpr)
                {
                    return new CharLiteralExpr(Tok(e.tok), (string)e.Value);
                }
                else if (e is StringLiteralExpr)
                {
                    var str = (StringLiteralExpr)e;
                    return new StringLiteralExpr(Tok(e.tok), (string)e.Value, str.IsVerbatim);
                }
                else if (e is TacticLiteralExpr)
                {
                    return new TacticLiteralExpr((string)e.Value);
                }
                else if (e.Value is Basetypes.BigDec)
                {
                    return new LiteralExpr(Tok(e.tok), (Basetypes.BigDec)e.Value);
                }
                else
                {
                    return new LiteralExpr(Tok(e.tok), (BigInteger)e.Value);
                }

            }
            else if (expr is ThisExpr)
            {
                if (expr is ImplicitThisExpr)
                {
                    return new ImplicitThisExpr(Tok(expr.tok));
                }
                else
                {
                    return new ThisExpr(Tok(expr.tok));
                }

            }
            else if (expr is IdentifierExpr)
            {
                var e = (IdentifierExpr)expr;
                return new IdentifierExpr(Tok(e.tok), e.Name);

            }
            else if (expr is DatatypeValue)
            {
                var e = (DatatypeValue)expr;
                return new DatatypeValue(Tok(e.tok), e.DatatypeName, e.MemberName, e.Arguments.ConvertAll(CloneExpr));

            }
            else if (expr is DisplayExpression)
            {
                DisplayExpression e = (DisplayExpression)expr;
                if (expr is SetDisplayExpr)
                {
                    return new SetDisplayExpr(Tok(e.tok), ((SetDisplayExpr)expr).Finite, e.Elements.ConvertAll(CloneExpr));
                }
                else if (expr is MultiSetDisplayExpr)
                {
                    return new MultiSetDisplayExpr(Tok(e.tok), e.Elements.ConvertAll(CloneExpr));
                }
                else
                {
                    Contract.Assert(expr is SeqDisplayExpr);
                    return new SeqDisplayExpr(Tok(e.tok), e.Elements.ConvertAll(CloneExpr));
                }

            }
            else if (expr is MapDisplayExpr)
            {
                MapDisplayExpr e = (MapDisplayExpr)expr;
                List<ExpressionPair> pp = new List<ExpressionPair>();
                foreach (ExpressionPair p in e.Elements)
                {
                    pp.Add(new ExpressionPair(CloneExpr(p.A), CloneExpr(p.B)));
                }
                return new MapDisplayExpr(Tok(expr.tok), e.Finite, pp);

            }
            else if (expr is NameSegment)
            {
                return CloneNameSegment(expr, newName);
            }
            else if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;
                return new ExprDotName(Tok(e.tok), CloneExpr(e.Lhs), e.SuffixName, e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
            }
            else if (expr is ApplySuffix)
            {
                var e = (ApplySuffix)expr;
                return CloneApplySuffix(e);
            }
            else if (expr is RevealExpr)
            {
                var e = (RevealExpr)expr;
                return new RevealExpr(Tok(e.tok), CloneExpr(e.Expr));
            }
            else if (expr is MemberSelectExpr)
            {
                var e = (MemberSelectExpr)expr;
                return new MemberSelectExpr(Tok(e.tok), CloneExpr(e.Obj), e.MemberName);

            }
            else if (expr is SeqSelectExpr)
            {
                var e = (SeqSelectExpr)expr;
                return new SeqSelectExpr(Tok(e.tok), e.SelectOne, CloneExpr(e.Seq), CloneExpr(e.E0), CloneExpr(e.E1));

            }
            else if (expr is MultiSelectExpr)
            {
                var e = (MultiSelectExpr)expr;
                return new MultiSelectExpr(Tok(e.tok), CloneExpr(e.Array), e.Indices.ConvertAll(CloneExpr));

            }
            else if (expr is SeqUpdateExpr)
            {
                var e = (SeqUpdateExpr)expr;
                return new SeqUpdateExpr(Tok(e.tok), CloneExpr(e.Seq), CloneExpr(e.Index), CloneExpr(e.Value));

            }
            else if (expr is DatatypeUpdateExpr)
            {
                var e = (DatatypeUpdateExpr)expr;
                return new DatatypeUpdateExpr(Tok(e.tok), CloneExpr(e.Root), e.Updates.ConvertAll(t => Tuple.Create(Tok(t.Item1), t.Item2, CloneExpr(t.Item3))));

            }
            else if (expr is FunctionCallExpr)
            {
                var e = (FunctionCallExpr)expr;
                return new FunctionCallExpr(Tok(e.tok), e.Name, CloneExpr(e.Receiver), e.OpenParen == null ? null : Tok(e.OpenParen), e.Args.ConvertAll(CloneExpr));

            }
            else if (expr is ApplyExpr)
            {
                var e = (ApplyExpr)expr;
                return new ApplyExpr(Tok(e.tok), CloneExpr(e.Function), e.Args.ConvertAll(CloneExpr));

            }
            else if (expr is MultiSetFormingExpr)
            {
                var e = (MultiSetFormingExpr)expr;
                return new MultiSetFormingExpr(Tok(e.tok), CloneExpr(e.E));

            }
            else if (expr is OldExpr)
            {
                var e = (OldExpr)expr;
                return new OldExpr(Tok(e.tok), CloneExpr(e.E));

            }
            else if (expr is UnchangedExpr)
            {
                var e = (UnchangedExpr)expr;
                return new UnchangedExpr(Tok(e.tok), e.Frame.ConvertAll(CloneFrameExpr));

            }
            else if (expr is UnaryOpExpr)
            {
                var e = (UnaryOpExpr)expr;
                return new UnaryOpExpr(Tok(e.tok), e.Op, CloneExpr(e.E));

            }
            else if (expr is ConversionExpr)
            {
                var e = (ConversionExpr)expr;
                return new ConversionExpr(Tok(e.tok), CloneExpr(e.E), CloneType(e.ToType));

            }
            else if (expr is BinaryExpr)
            {
                var e = (BinaryExpr)expr;
                return new BinaryExpr(Tok(e.tok), e.Op, CloneExpr(e.E0,newName), CloneExpr(e.E1));

            }
            else if (expr is TernaryExpr)
            {
                var e = (TernaryExpr)expr;
                return new TernaryExpr(Tok(e.tok), e.Op, CloneExpr(e.E0), CloneExpr(e.E1), CloneExpr(e.E2));

            }
            else if (expr is ChainingExpression)
            {
                var e = (ChainingExpression)expr;
                return CloneExpr(e.E);  // just clone the desugaring, since it's already available

            }
            else if (expr is LetExpr)
            {
                var e = (LetExpr)expr;
                return new LetExpr(Tok(e.tok), e.LHSs.ConvertAll(CloneCasePattern), e.RHSs.ConvertAll(CloneExpr), CloneExpr(e.Body), e.Exact, e.Attributes);

            }
            else if (expr is NamedExpr)
            {
                var e = (NamedExpr)expr;
                return new NamedExpr(Tok(e.tok), e.Name, CloneExpr(e.Body));
            }
            else if (expr is ComprehensionExpr)
            {
                var e = (ComprehensionExpr)expr;
                var tk = Tok(e.tok);
                var bvs = e.BoundVars.ConvertAll(CloneBoundVar);
                var range = CloneExpr(e.Range);
                var term = CloneExpr(e.Term);
                if (e is QuantifierExpr)
                {
                    var q = (QuantifierExpr)e;
                    var tvs = q.TypeArgs.ConvertAll(CloneTypeParam);
                    if (e is ForallExpr)
                    {
                        return new ForallExpr(tk, tvs, bvs, range, term, CloneAttributes(e.Attributes));
                    }
                    else if (e is ExistsExpr)
                    {
                        return new ExistsExpr(tk, tvs, bvs, range, term, CloneAttributes(e.Attributes));
                    }
                    else
                    {
                        Contract.Assert(false); throw new cce.UnreachableException();  // unexpected quantifier expression
                    }
                }
                else if (e is MapComprehension)
                {
                    return new MapComprehension(tk, ((MapComprehension)e).Finite, bvs, range, term, CloneAttributes(e.Attributes));
                }
                else if (e is LambdaExpr)
                {
                    var l = (LambdaExpr)e;
                    return new LambdaExpr(tk, l.OneShot, bvs, range, l.Reads.ConvertAll(CloneFrameExpr), term);
                }
                else
                {
                    Contract.Assert(e is SetComprehension);
                    var tt = (SetComprehension)e;
                    return new SetComprehension(tk, tt.Finite, bvs, range, tt.TermIsImplicit ? null : term, CloneAttributes(e.Attributes));
                }

            }
            else if (expr is WildcardExpr)
            {
                return new WildcardExpr(Tok(expr.tok));

            }
            else if (expr is StmtExpr)
            {
                var e = (StmtExpr)expr;
                return new StmtExpr(Tok(e.tok), CloneStmt(e.S), CloneExpr(e.E));

            }
            else if (expr is ITEExpr)
            {
                var e = (ITEExpr)expr;
                return new ITEExpr(Tok(e.tok), e.IsExistentialGuard, CloneExpr(e.Test), CloneExpr(e.Thn), CloneExpr(e.Els));

            }
            else if (expr is AutoGeneratedExpression)
            {
                var e = (AutoGeneratedExpression)expr;
                var a = CloneExpr(e.E);
                return new AutoGeneratedExpression(Tok(e.tok), a);

            }
            else if (expr is ParensExpression)
            {
                var e = (ParensExpression)expr;
                return CloneExpr(e.E);  // skip the parentheses in the clone

            }
            else if (expr is MatchExpr)
            {
                var e = (MatchExpr)expr;
                return new MatchExpr(Tok(e.tok), CloneExpr(e.Source), e.Cases.ConvertAll(CloneMatchCaseExpr), e.UsesOptionalBraces);

            }
            else if (expr is NegationExpression)
            {
                var e = (NegationExpression)expr;
                return new NegationExpression(Tok(e.tok), CloneExpr(e.E));

            }
            else
            {
                Contract.Assert(false); throw new cce.UnreachableException();  // unexpected expression
            }
        }

        public Statement CloneStmt(Statement stmt, Expression expr)
        {
            if (stmt == null)
            {
                return null;
            }

            Statement r;
            if (stmt is AssertStmt)
            {
                var s = (AssertStmt)stmt;
                r = new AssertStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Expr), CloneBlockStmt(s.Proof), null);

            }
            else if (stmt is AssumeStmt)
            {
                var s = (AssumeStmt)stmt;
                r = new AssumeStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Expr), null);

            }
            else if (stmt is TacticAssertStmt)
            {
                var s = (TacticAssertStmt)stmt;
                r = new TacticAssertStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Expr), null, s.IsObjectLevel);

            }
            else if (stmt is PrintStmt)
            {
                var s = (PrintStmt)stmt;
                r = new PrintStmt(Tok(s.Tok), Tok(s.EndTok), s.Args.ConvertAll(CloneExpr));

            }
            else if (stmt is RevealStmt)
            {
                var s = (RevealStmt)stmt;
                r = new RevealStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Expr));

            }
            else if (stmt is BreakStmt)
            {
                var s = (BreakStmt)stmt;
                if (s.TargetLabel != null)
                {
                    r = new BreakStmt(Tok(s.Tok), Tok(s.EndTok), s.TargetLabel);
                }
                else
                {
                    r = new BreakStmt(Tok(s.Tok), Tok(s.EndTok), s.BreakCount);
                }

            }
            else if (stmt is ReturnStmt)
            {
                var s = (ReturnStmt)stmt;
                r = new ReturnStmt(Tok(s.Tok), Tok(s.EndTok), s.rhss == null ? null : s.rhss.ConvertAll(CloneRHS));

            }
            else if (stmt is YieldStmt)
            {
                var s = (YieldStmt)stmt;
                r = new YieldStmt(Tok(s.Tok), Tok(s.EndTok), s.rhss == null ? null : s.rhss.ConvertAll(CloneRHS));

            }
            else if (stmt is AssignStmt)
            {
                var s = (AssignStmt)stmt;
                r = new AssignStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Lhs), CloneRHS(s.Rhs));

            }
            else if (stmt is BlockStmt)
            {
                r = CloneBlockStmt((BlockStmt)stmt);

            }
            else if (stmt is IfStmt)
            {
                var s = (IfStmt)stmt;
                r = new IfStmt(Tok(s.Tok), Tok(s.EndTok), s.IsExistentialGuard, CloneExpr(s.Guard), CloneBlockStmt(s.Thn), CloneStmt(s.Els));

            }
            else if (stmt is AlternativeStmt)
            {
                var s = (AlternativeStmt)stmt;
                r = new AlternativeStmt(Tok(s.Tok), Tok(s.EndTok), s.Alternatives.ConvertAll(CloneGuardedAlternative), s.UsesOptionalBraces);

            }
            else if (stmt is WhileStmt)
            {
                var s = (WhileStmt)stmt;
                r = new WhileStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Guard), s.Invariants.ConvertAll(CloneMayBeFreeExpr), s.TInvariants.ConvertAll(CloneStmt), CloneSpecExpr(s.Decreases), CloneSpecFrameExpr(s.Mod), CloneBlockStmt(s.Body));

            }
            else if (stmt is AlternativeLoopStmt)
            {
                var s = (AlternativeLoopStmt)stmt;
                r = new AlternativeLoopStmt(Tok(s.Tok), Tok(s.EndTok), s.Invariants.ConvertAll(CloneMayBeFreeExpr), CloneSpecExpr(s.Decreases), CloneSpecFrameExpr(s.Mod), s.Alternatives.ConvertAll(CloneGuardedAlternative), s.UsesOptionalBraces);

            }
            else if (stmt is ForallStmt)
            {
                var s = (ForallStmt)stmt;
                r = new ForallStmt(Tok(s.Tok), Tok(s.EndTok), s.BoundVars.ConvertAll(CloneBoundVar), null, CloneExpr(s.Range), s.Ens.ConvertAll(CloneMayBeFreeExpr), CloneStmt(s.Body));
                if (s.ForallExpressions != null)
                {
                    ((ForallStmt)r).ForallExpressions = s.ForallExpressions.ConvertAll(CloneExpr);
                }
            }
            else if (stmt is CalcStmt)
            {
                var s = (CalcStmt)stmt;
                // calc statements have the unusual property that the last line is duplicated.  If that is the case (which
                // we expect it to be here), we share the clone of that line as well.
                var lineCount = s.Lines.Count;
                var lines = new List<Expression>(lineCount);
                for (int i = 0; i < lineCount; i++)
                {
                    lines.Add(i == lineCount - 1 && 2 <= lineCount && s.Lines[i] == s.Lines[i - 1] ? lines[i - 1] : CloneExpr(s.Lines[i]));
                }
                Contract.Assert(lines.Count == lineCount);
                r = new CalcStmt(Tok(s.Tok), Tok(s.EndTok), CloneCalcOp(s.Op), lines, s.Hints.ConvertAll(CloneBlockStmt), s.StepOps.ConvertAll(CloneCalcOp), CloneCalcOp(s.ResultOp), CloneAttributes(s.Attributes));

            }
            else if (stmt is MatchStmt)
            {
                var s = (MatchStmt)stmt;
                r = new MatchStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Source), s.Cases.ConvertAll(CloneMatchCaseStmt), s.UsesOptionalBraces);

            }
            else if (stmt is AssignSuchThatStmt)
            {
                var s = (AssignSuchThatStmt)stmt;
                r = new AssignSuchThatStmt(Tok(s.Tok), Tok(s.EndTok), s.Lhss.ConvertAll(CloneExpr), CloneExpr(s.Expr), s.AssumeToken == null ? null : Tok(s.AssumeToken), null);

            }
            else if (stmt is UpdateStmt)
            {
                var s = (UpdateStmt)stmt;
                r = new UpdateStmt(Tok(s.Tok), Tok(s.EndTok), s.Lhss.ConvertAll(CloneExpr), s.Rhss.ConvertAll(CloneRHS), s.CanMutateKnownState);

            }
            else if (stmt is VarDeclStmt)
            {
                var s = (VarDeclStmt)stmt;
                var lhss = s.Locals.ConvertAll(c => new LocalVariable(Tok(c.Tok), Tok(c.EndTok), c.Name, CloneType(c.OptionalType), c.IsGhost));
                r = new VarDeclStmt(Tok(s.Tok), Tok(s.EndTok), lhss, (ConcreteUpdateStatement)CloneStmt(s.Update));

            }
            else if (stmt is LetStmt)
            {
                var s = (LetStmt)stmt;
                r = new LetStmt(Tok(s.Tok), Tok(s.EndTok), s.LHSs.ConvertAll(CloneCasePattern), s.RHSs.ConvertAll(CloneExpr));

            }
            else if (stmt is ModifyStmt)
            {
                var s = (ModifyStmt)stmt;
                var mod = CloneSpecFrameExpr(s.Mod);
                var body = s.Body == null ? null : CloneBlockStmt(s.Body);
                r = new ModifyStmt(Tok(s.Tok), Tok(s.EndTok), mod.Expressions, mod.Attributes, body);

            }
            else if (stmt is TacticCasesBlockStmt)
            {
                var s = (TacticCasesBlockStmt)stmt;
                var guard = CloneExpr(s.Guard);
                var body = s.Body == null ? null : CloneBlockStmt(s.Body);
                var attrs = s.Attributes == null ? null : CloneAttributes(s.Attributes);
                r = new TacticCasesBlockStmt(Tok(s.Tok), Tok(s.EndTok), guard, attrs, body);

                /*      } else if (stmt is TacnyChangedBlockStmt) {
                        var s = (TacnyChangedBlockStmt)stmt;
                        var body = s.Body == null ? null : CloneBlockStmt(s.Body);
                        r = new TacnyChangedBlockStmt(Tok(s.Tok), Tok(s.EndTok), body);

                      } else if (stmt is TacnySolvedBlockStmt) {
                        var s = (TacnySolvedBlockStmt)stmt;
                        var body = s.Body == null ? null : CloneBlockStmt(s.Body);
                        r = new TacnySolvedBlockStmt(Tok(s.Tok), Tok(s.EndTok), body);

                      } else if (stmt is TacnyTryCatchBlockStmt) {
                        var s = (TacnyTryCatchBlockStmt)stmt;
                        var body = s.Body == null ? null : CloneBlockStmt(s.Body);
                        var c = s.Ctch == null ? null : CloneBlockStmt(s.Ctch);
                        r = new TacnyTryCatchBlockStmt(Tok(s.Tok), Tok(s.EndTok), body, c);
                */
            }
            else if (stmt is TacticVarDeclStmt)
            {
                var s = (TacticVarDeclStmt)stmt;
                var lhss = s.Locals.ConvertAll(c => new LocalVariable(Tok(c.Tok), Tok(c.EndTok), c.Name, CloneType(c.OptionalType), c.IsGhost));
                r = new TacticVarDeclStmt(Tok(s.Tok), Tok(s.EndTok), lhss, (ConcreteUpdateStatement)CloneStmt(s.Update));

            }
            else if (stmt is TacticForallStmt)
            {
                var s = (TacticForallStmt)stmt;
                r = new TacticForallStmt(Tok(s.Tok), Tok(s.EndTok), CloneExpr(s.Spec), CloneBlockStmt((BlockStmt)s.Body), CloneAttributes(s.Attributes));
            }
            else if (stmt is TacticTryBlockStmt)
            {
                var s = (TacticTryBlockStmt)stmt;
                r = new TacticTryBlockStmt(Tok(s.Tok), Tok(s.EndTok), CloneBlockStmt((BlockStmt)s.Body));
            }
            else if (stmt is InlineTacticBlockStmt)
            {
                var s = (InlineTacticBlockStmt)stmt;
                r = new InlineTacticBlockStmt(Tok(s.Tok), Tok(s.EndTok), CloneAttributes(s.Attributes), CloneBlockStmt((BlockStmt)s.Body));
            }
            else
            {
                Contract.Assert(false); throw new cce.UnreachableException();  // unexpected statement
            }

            // add labels to the cloned statement
            AddStmtLabels(r, stmt.Labels);
            r.Attributes = CloneAttributes(stmt.Attributes);

            return r;
        }

        public  Expression CloneNameSegment(Expression expr, String newName)
        {
            var e = (NameSegment)expr;
            return new NameSegment(Tok(e.tok), newName, e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
        }

        public Formal CloneFormal(Formal formal, String newName)
        {
            Formal f = new Formal(Tok(formal.tok), newName, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            //if (f.Type is UserDefinedType && formal.Type is UserDefinedType)
            //    ((UserDefinedType)f.Type).ResolvedClass = ((UserDefinedType)(formal.Type)).ResolvedClass;
            return f;
        }

    }
}
/* 
 * foreach (Statement s in ((BlockStmt)stmt).Body) {
          Indent(ind);
          PrintStatement(s, ind);
          wr.WriteLine();
        }

    */
