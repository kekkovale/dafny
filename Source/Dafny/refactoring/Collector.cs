using Microsoft.Dafny.Tacny;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.refactoring
{
    class Collector : Cloner
    {
        HashSet<int> tokMap;
        private String compiledName;
        private Dictionary<int, MemberDecl> updates;
        private Program resolvedProgram;
        private Program program;
        private Finder finder;
        private Predicate predicate = null;
        private myPredicate myPredicate;

        internal Finder Finder
        {
            get
            {
                return finder;
            }
        }

        public Collector(Program program, Program resolvedProgram)
        {
            tokMap = new HashSet<int>();
            this.program = program;
            this.resolvedProgram = resolvedProgram;
            updates = new Dictionary<int, MemberDecl>();
            finder = new Finder(resolvedProgram);
        }

        public HashSet<int> collectVariables(int line, int column)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));
            compiledName = finder.findExpression(line, column);
           
            //ClassDecl classDecl = resolvedProgram.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
            foreach (TopLevelDecl t in resolvedProgram.DefaultModuleDef.TopLevelDecls)
            {
                ClassDecl classDecl = t as ClassDecl;

                foreach (MemberDecl member in classDecl.Members)
                {
                    if (refactoringFormals())
                    {
                        //only if memeber.name is equals to current member name
                        if (member.Name == finder.CurrentMemberName && member.Name != null && finder.CurrentMemberName != null)
                        {
                            CloneMember(member);
                        }

                    }
                    else
                    {
                        CloneMember(member);
                    }
                }
            }
            return this.tokMap;
        }

        public Predicate collectPredicate(String newName, int line)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));
            Predicate newPredicate = null;
            String memberName = finder.findFoldPredicate(line);

            if(memberName != null)
            {
                newPredicate = createNewPredicate(newName, memberName, line);
                createPredicateCaller(memberName, line);
            }

            return newPredicate;
        }

        private void createPredicateCaller(String memberName,int line)
        {
            int index = -1;
            MaybeFreeExpression mfe = null;
            ClassDecl classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            foreach (MemberDecl member in classDecl.Members)
            {
                if (member.Name == memberName)
                {
                    if (member is Method)
                    {
                        Method m = member as Method;

                        foreach (MaybeFreeExpression e in m.Ens)
                        {
                            if (e.E.tok.line == line && predicate != null)
                            {
                                index = m.Ens.IndexOf(e);
                                NameSegment lhs = new NameSegment(Tok(predicate.tok), predicate.Name, null);
                                ApplySuffix a = new ApplySuffix(Tok(predicate.tok), lhs, myPredicate.Args.ConvertAll(CloneExpr));

                                mfe = new MaybeFreeExpression(CloneExpr(a), e.IsFree);
                            }

                        }

                        if (mfe != null && index != -1)
                        {
                            m.Ens[index] = mfe;
                        }

                        mfe = null;

                        foreach (Statement s in m.Body.Body)
                        {
                            if (s is WhileStmt)
                            {
                                var stmt = (WhileStmt)s;
                                foreach (MaybeFreeExpression e1 in stmt.Invariants)
                                {
                                    if (e1.E.tok.line == line && predicate != null)
                                    {
                                        index = stmt.Invariants.IndexOf(e1);
                                        NameSegment lhs = new NameSegment(Tok(predicate.tok), predicate.Name, null);
                                        ApplySuffix a = new ApplySuffix(Tok(predicate.tok), lhs, myPredicate.Args.ConvertAll(CloneExpr));

                                        mfe = new MaybeFreeExpression(CloneExpr(a), e1.IsFree);
                                    }
                                }

                                if (mfe != null && index != -1)
                                {
                                    stmt.Invariants[index] = mfe;
                                }
                                mfe = null;
                            }
                            else if (s is AlternativeLoopStmt)
                            {
                                var stmt = (AlternativeLoopStmt)s;
                                foreach (MaybeFreeExpression e2 in stmt.Invariants)
                                {
                                    if (e2.E.tok.line == line)
                                    {
                                        index = stmt.Invariants.IndexOf(e2);
                                        NameSegment lhs = new NameSegment(Tok(predicate.tok), predicate.Name, null);
                                        ApplySuffix a = new ApplySuffix(Tok(predicate.tok), lhs, myPredicate.Args.ConvertAll(CloneExpr));

                                        mfe = new MaybeFreeExpression(CloneExpr(a), e2.IsFree);
                                    }
                                }

                                if (mfe != null && index != -1)
                                {
                                    stmt.Invariants[index] = mfe;
                                }
                                mfe = null;
                            }
                            else if (s is ForallStmt)
                            {
                                var stmt = (ForallStmt)s;
                                foreach (MaybeFreeExpression e3 in stmt.Ens)
                                {
                                    if (e3.E.tok.line == line)
                                    {
                                        index = stmt.Ens.IndexOf(e3);
                                        NameSegment lhs = new NameSegment(Tok(predicate.tok), predicate.Name, null);
                                        ApplySuffix a = new ApplySuffix(Tok(predicate.tok), lhs, myPredicate.Args.ConvertAll(CloneExpr));

                                        mfe = new MaybeFreeExpression(CloneExpr(a), e3.IsFree);
                                    }
                                }

                                if (mfe != null && index != -1)
                                {
                                    stmt.Ens[index] = mfe;
                                }
                                mfe = null;
                            }
                        }
                    }
                }

            }
            
        }



        private bool refactoringFormals()
        {            
            if (finder.CurrentMemberName != null)
                return true;
            else
                return false;
        }

        private bool matchName(String compiledName)
        {
            return this.compiledName == compiledName;
        }

        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();

            foreach (LocalVariable lv in s.Locals)
            {

                if (matchName(lv.CompileName))
                {
                    tokMap.Add(lv.Tok.pos);
                }

            }

            return lhss;
        }


        public override Statement CloneStmt(Statement stmt)
        {
            if (stmt is VarDeclStmt)
            {
                var s = (VarDeclStmt)stmt;
                List<LocalVariable> lhss = cloneLocalVariables(s);

                return new VarDeclStmt(Tok(s.Tok), Tok(s.EndTok), lhss, (ConcreteUpdateStatement)CloneStmt(s.Update));
            }
            else
                return base.CloneStmt(stmt);
        }

        public override Expression CloneNameSegment(Expression expr)
        {
            var nameSegment = expr as NameSegment;

            if (matchName(finder.getCompileName(nameSegment.ResolvedExpression)))
            {
                tokMap.Add(nameSegment.tok.pos);
            }

            return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {

            if (matchName(finder.getCompileName(formal)))
                tokMap.Add(formal.tok.pos);

            return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {

            if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;


                if (matchName(finder.getCompileName(e.ResolvedExpression)))
                    tokMap.Add(e.tok.pos);
                
            }
            
            return base.CloneExpr(expr);

        }

        public override MemberDecl CloneMember(MemberDecl member)
        {

            if (member is Field)
            {
                if (member is ConstantField)
                {
                    var c = (ConstantField)member;

                    if (matchName(finder.getCompileName(c)))
                        tokMap.Add(c.tok.pos);

                }
                else
                {
                    Contract.Assert(!(member is SpecialField));
                    var f = (Field)member;

                    if (matchName(finder.getCompileName(f)))
                        tokMap.Add(f.tok.pos);

                }
            }

            return base.CloneMember(member);

        }

        public override Function CloneFunction(Function f, string newName = null)
        {
            var tps = f.TypeArgs.ConvertAll(CloneTypeParam);
            var formals = f.Formals.ConvertAll(CloneFormal);
            var req = f.Req.ConvertAll(CloneExpr);
            var reads = f.Reads.ConvertAll(CloneFrameExpr);
            var decreases = CloneSpecExpr(f.Decreases);
            var ens = f.Ens.ConvertAll(CloneExpr);
            Expression body;
            body = CloneExpr(f.Body);

            if (matchName(finder.getCompileName(f)))
            {
                tokMap.Add(f.tok.pos);
            }

            return base.CloneFunction(f);
        }

        public Predicate createNewPredicate(String predicateName, String memberName, int line)
        {
            myPredicate = new myPredicate();

            Boogie.IToken start= null;
            Boogie.IToken end = null;
            ClassDecl classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            foreach (MemberDecl member in classDecl.Members)
            {
                if(member.Name == memberName)
                {
                    start = TokenGenerator.NextToken(member.BodyStartTok, member.BodyStartTok);
                    end = TokenGenerator.NextToken(member.BodyEndTok, member.BodyEndTok);

                    if (member is Method)
                    {
                        Method m = member as Method;

                        foreach (MaybeFreeExpression e in m.Ens)
                        {
                            if (e.E.tok.line == line)
                            {
                                Expression body = CloneExpr(e.E);
                                myPredicate.Body = body;
                            }
                        }

                        foreach (Statement s in m.Body.Body)
                        {
                            if (s is WhileStmt)
                            {
                                var stmt = (WhileStmt)s;
                                foreach (MaybeFreeExpression e1 in stmt.Invariants)
                                {
                                    if (e1.E.tok.line == line)
                                    {
                                        Expression body = CloneExpr(e1.E);
                                        myPredicate.Body = body;
                                    }
                                }
                            }
                            else if (s is AlternativeLoopStmt)
                            {
                                var stmt = (AlternativeLoopStmt)s;
                                foreach (MaybeFreeExpression e2 in stmt.Invariants)
                                {
                                    if (e2.E.tok.line == line)
                                    {
                                        Expression body = CloneExpr(e2.E);
                                        myPredicate.Body = body;
                                    }
                                }
                            }
                            else if (s is ForallStmt)
                            {
                                var stmt = (ForallStmt)s;
                                foreach (MaybeFreeExpression e3 in stmt.Ens)
                                {
                                    if (e3.E.tok.line == line)
                                    {
                                        Expression body = CloneExpr(e3.E);
                                        myPredicate.Body = body;
                                    }
                                }
                            }
                        }

                        foreach (MaybeFreeExpression e4 in m.Req)
                        {
                            myPredicate.Req.Add(CloneExpr(e4.E));
                        }

                        foreach (KeyValuePair<String, Type> entry in Finder.PredicateVariables)
                        {
                            Formal f = new Formal(Tok(start), entry.Key, CloneType(entry.Value), true, false, false);
                            myPredicate.Formals.Add(f);
                        }

                        /*
                        foreach (Formal e5 in m.Ins)
                        {
                            
                            myPredicate.Formals.Add(CloneFormal(e5));
                        }

                        foreach (Formal e6 in m.Outs)
                        {
                            myPredicate.Formals.Add(CloneFormal(e6));
                        }
                        */
                        foreach (Formal e7 in myPredicate.Formals)
                        {
                            NameSegment nmsegm = new NameSegment(Tok(e7.tok), e7.DisplayName, null);
                            myPredicate.Args.Add(nmsegm);
                        }

                        myPredicate.TypeArgs = m.TypeArgs;
                        myPredicate.Attributes = m.Attributes;
                        myPredicate.Decreases = m.Decreases;
                        //FrameExpression reads = new FrameExpression(null, CloneExpr(frame.E), frame.FieldName);
                        myPredicate.Reads = getReads(m.Ins);
                    }
                }
            }
            return predicate = new Predicate(Tok(end), predicateName, false, false, true,myPredicate.TypeArgs, myPredicate.Formals,
                      myPredicate.Req, myPredicate.Reads, myPredicate.Ens, myPredicate.Decreases, myPredicate.Body, Predicate.BodyOriginKind.OriginalOrInherited, myPredicate.Attributes, null, null);

        }

        private List<FrameExpression> getReads(List<Formal> formals)
        {
            List<FrameExpression> reads = new List<FrameExpression>();
            foreach (Formal e2 in myPredicate.Formals)
            {
                if (e2.Type is UserDefinedType)
                {
                    Boogie.IToken tok = TokenGenerator.NextToken(e2.tok, e2.tok);
                    NameSegment nmsegm  = new NameSegment(Tok(tok), e2.DisplayName, null);
                    FrameExpression expr = new FrameExpression(Tok(nmsegm.tok), nmsegm, null);
                    reads.Add(expr);
                }
            }

            return reads;
        }

        public override Method CloneMethod(Method m)
        {
            var tps = m.TypeArgs.ConvertAll(CloneTypeParam);
            var ins = m.Ins.ConvertAll(CloneFormal);
            var req = m.Req.ConvertAll(CloneMayBeFreeExpr);
            var mod = CloneSpecFrameExpr(m.Mod);
            var decreases = CloneSpecExpr(m.Decreases);

            var ens = m.Ens.ConvertAll(CloneMayBeFreeExpr);

            BlockStmt body = CloneMethodBody(m);

            if (matchName(finder.getCompileName(m)))
            {
                tokMap.Add(m.tok.pos);
            }

            return base.CloneMethod(m);
        }

    }
}
