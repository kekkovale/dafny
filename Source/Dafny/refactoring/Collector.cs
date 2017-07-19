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
        private Finder finder;

        internal Finder Finder
        {
            get
            {
                return finder;
            }
        }

        public Collector(Program program)
        {
            tokMap = new HashSet<int>();
            this.resolvedProgram = program;
            updates = new Dictionary<int, MemberDecl>();
            finder = new Finder(program);
        }

        public HashSet<int> collectVariables(String compiledName)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));

            this.compiledName = compiledName;
            ClassDecl classDecl = resolvedProgram.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            foreach (MemberDecl member in classDecl.Members)
            {
                if (refactoringFormals())
                {

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

            return this.tokMap;
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
