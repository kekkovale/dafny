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
        private Program resolvedProgram;  
        private int line;
        private int column;
        private ClassDecl classDecl;

        public Finder(Program program)
        {

            this.resolvedProgram = program;
            this.classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
            currentMemberName = null;
            exprFound = false;
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


    }
}
