using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.refactoring
{
    class Finder : Cloner
    {
        
        private String compiledName;
        private String displayedName;
        private String currentMemberName;
        private bool exprFound;
        private bool findingPredicateVar;
        private Program program;  
        private int line;
        private int column;
        private ClassDecl classDecl;
        private Dictionary<String, Type> predicateVariables;

        public Finder(Program program)
        {
            this.program = program;
            this.classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
            currentMemberName = null;
            exprFound = false;
            predicateVariables = new Dictionary<string, Type>();
            findingPredicateVar = false;
        }

        public String findExpression(int line, int column)
        {
            currentMemberName = null;
            this.line = line;
            this.column = column;
            
            foreach (MemberDecl member in classDecl.Members)
            {
                CloneMember(member);

                if (displayedName == compiledName && displayedName != null && compiledName != null)
                {
                    currentMemberName = member.Name;
                }

                if (exprFound)
                    break;
            }

            return this.compiledName;

        }

        public String findFoldPredicate(int line)
        {
            
            this.classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            this.line = line;
            findingPredicateVar = true;
            foreach (MemberDecl member in classDecl.Members)
            {
                if (member is Method)
                {
                    Method m = member as Method;

                    foreach (MaybeFreeExpression e in m.Ens)
                    {
                        if (e.E.tok.line == this.line)
                        {
                            CloneExpr(e.E);
                            exprFound = true;
                        }
                    }

                    foreach (Statement s in m.Body.Body)
                    {
                        if (s is WhileStmt)
                        {
                            var stmt = (WhileStmt)s;
                            foreach (MaybeFreeExpression e1 in stmt.Invariants)
                            {
                                if (e1.E.tok.line == this.line)
                                {
                                    CloneExpr(e1.E);
                                    exprFound = true;
                                }
                            }
                        }
                        else if (s is AlternativeLoopStmt)
                        {
                            var stmt = (AlternativeLoopStmt)s;
                            foreach (MaybeFreeExpression e2 in stmt.Invariants)
                            {
                                if (e2.E.tok.line == this.line)
                                {
                                    CloneExpr(e2.E);
                                    exprFound = true;
                                }
                            }
                        }
                        else if (s is ForallStmt)
                        {
                            var stmt = (ForallStmt)s;
                            foreach (MaybeFreeExpression e3 in stmt.Ens)
                            {
                                if (e3.E.tok.line == this.line)
                                {
                                    CloneExpr(e3.E);
                                    exprFound = true;
                                }
                            }
                        }
                    }
                    
                }

                if (exprFound)
                    return member.Name;
            }
            findingPredicateVar = false;

            return null;

        }


        public String getCompileName<T>(T expr)
        {
            String compileName = null;

            if (expr is IdentifierExpr)
            {
                IdentifierExpr matchExpr = expr as IdentifierExpr;
                compileName = matchExpr.Var.CompileName;

            }
            else if (expr is Formal)
            {
                Formal matchExpr = expr as Formal;
                compileName = matchExpr.CompileName;
            }
            else if (expr is LocalVariable)
            {
                LocalVariable matchExpr = expr as LocalVariable;
                compileName = matchExpr.CompileName;
            }
            else if (expr is MemberSelectExpr)
            {
                MemberSelectExpr matchExpr = expr as MemberSelectExpr;
                compileName = matchExpr.Member.FullCompileName;
            }
            else if (expr is ConstantField)
            {
                ConstantField matchExpr = expr as ConstantField;
                compileName = matchExpr.FullCompileName;
            }
            else if (expr is Field)
            {
                Field matchExpr = expr as Field;
                compileName = matchExpr.FullCompileName;
            }
            else if (expr is Method)
            {
                Method matchExpr = expr as Method;
                compileName = matchExpr.FullCompileName;
            }
            else if (expr is Predicate)
            {
                Predicate matchExpr = expr as Predicate;
                compileName = matchExpr.FullCompileName;
            }

            return compileName;
        }

        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();

            foreach (LocalVariable lv in s.Locals)
            {

                if (lv.Tok.line == this.line && lv.Tok.col == this.column)
                { 
                    this.compiledName = this.getCompileName(lv);
                    this.displayedName = lv.DisplayName;
                    this.exprFound = true;
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

            if (findingPredicateVar)
            {
                if (nameSegment.ResolvedExpression is MemberSelectExpr)
                {
                    MemberSelectExpr matchExpr = nameSegment.ResolvedExpression as MemberSelectExpr;
                    if (!(matchExpr.Member is Function) && !(matchExpr.Member is Field) && !(matchExpr.Member is Method))
                    {
                        if (!predicateVariables.ContainsKey(nameSegment.Name))
                            predicateVariables.Add(nameSegment.Name, nameSegment.Type);
                    }

                }
                else if (nameSegment.ResolvedExpression is IdentifierExpr)
                {
                    IdentifierExpr matchExpr = nameSegment.ResolvedExpression as IdentifierExpr;

                    if(!(matchExpr.Var is BoundVar))
                    {
                        if (!predicateVariables.ContainsKey(nameSegment.Name))
                            predicateVariables.Add(nameSegment.Name, matchExpr.Var.Type);
                    }
                    
                    
                }
                else
                {
                    if (!predicateVariables.ContainsKey(nameSegment.Name))
                        predicateVariables.Add(nameSegment.Name, nameSegment.Type);
                }

            }
            

            if (nameSegment.tok.line == this.line && nameSegment.tok.col == this.column)
            {
                this.compiledName = this.getCompileName(nameSegment.ResolvedExpression);
                this.displayedName = nameSegment.Name;
                this.exprFound = true;
            }

            return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {

            if (formal.tok.line == this.line && formal.tok.col == this.column)
            {
                this.compiledName = this.getCompileName(formal);
                this.displayedName = formal.Name;
                this.exprFound = true;
            }

            return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {

            if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;
                /*
                if (findingPredicateVar)
                {
                    predicateVariables.Add(e.SuffixName, e.Type);
                }
                */
                if (e.tok.line == this.line && e.tok.col == this.column)
                {
                    this.compiledName = this.getCompileName(e.ResolvedExpression);
                    this.displayedName = e.SuffixName;
                    this.exprFound = true;
                }
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

                    if (c.tok.line == this.line && c.tok.col == this.column)
                    {
                        this.compiledName = this.getCompileName(c);
                        this.displayedName = c.Name;
                        this.exprFound = true;
                    }
                }
                else
                {
                    Contract.Assert(!(member is SpecialField));
                    var f = (Field)member;

                    if (f.tok.line == this.line && f.tok.col == this.column)
                    {
                        this.compiledName = this.getCompileName(f);
                        this.displayedName = f.Name;
                        this.exprFound = true;
                    }


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

            if (f.tok.line == this.line && f.tok.col == this.column)
            {
                this.compiledName = this.getCompileName(f);
                this.exprFound = true;
            }
            
            return base.CloneFunction(f);
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


            if (m.tok.line == this.line && m.tok.col == this.column)
            {
                this.compiledName = this.getCompileName(m);
                this.exprFound = true;
            }

            return base.CloneMethod(m);
        }

        public string CompiledName
        {
            get
            {
                return compiledName;
            }

        }

        public string DisplayedName
        {
            get
            {
                return displayedName;
            }

        }

        public string CurrentMemberName
        {
            get
            {
                return currentMemberName;
            }

        }

        public Dictionary<string, Type> PredicateVariables
        {
            get
            {
                return predicateVariables;
            }

            set
            {
                predicateVariables = value;
            }
        }
    }
}
