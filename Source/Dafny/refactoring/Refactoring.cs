using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.refactoring
{
    class Refactoring : Cloner
    {               
        private String newName;
        private Dictionary<int, MemberDecl> updates;
        private Program program;
        private Finder finder;

        public Refactoring(Program program)
        {            
            this.program = program;
            updates = new Dictionary<int, MemberDecl>();
            finder = new Finder(program);
        }

        public Program renameRefactoring(String newName, int line, int column)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));

            this.newName = newName;
            ClassDecl classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            if (finder.findExpression(line, column))
            {
                foreach (MemberDecl member in classDecl.Members)
                {
                    if (refactoringFormals())
                    {
                        if (member.Name == finder.CurrentMethodName && member.Name != null && finder.CurrentMethodName != null)
                        {
                            MemberDecl newMember = CloneMember(member);
                            int index = classDecl.Members.IndexOf(member);
                            updates.Add(index, newMember);
                            
                        }
                    }
                    else
                    {
                        MemberDecl newMember = CloneMember(member);
                        int index = classDecl.Members.IndexOf(member);
                        updates.Add(index, newMember);
                    }
                }
                this.updateProgram(classDecl);
            }

            return program;
        }

        private bool refactoringFormals()
        {
            if (finder.CurrentMethodName != null)
                return true;
            else
                return false;
        }           

        private void updateProgram(ClassDecl classDecl)
        {

            foreach (KeyValuePair<int, MemberDecl> entry in updates)
            {
                classDecl.Members[entry.Key] = entry.Value;
                //classDecl.Members.Insert(entry.Key,entry.Value);
            }
        }

        private bool isOldValue(String compiledName)
        {
            return finder.CompiledName == compiledName;
        }

        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();
            
            foreach (LocalVariable lv in s.Locals)
            {              
                
                if (isOldValue(lv.CompileName))
                {
                    var newLv = new LocalVariable(Tok(lv.Tok), Tok(lv.EndTok), this.newName, CloneType(lv.OptionalType), lv.IsGhost);
                    lhss.Add(newLv);
                }
                else
                {
                    var newLv = new LocalVariable(Tok(lv.Tok), Tok(lv.EndTok), lv.Name, CloneType(lv.OptionalType), lv.IsGhost);
                    lhss.Add(newLv);
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

            if (isOldValue(finder.getCompileName(nameSegment.ResolvedExpression)))
            {
                return new NameSegment(Tok(nameSegment.tok), this.newName, nameSegment.OptTypeArguments == null ? null : nameSegment.OptTypeArguments.ConvertAll(CloneType));
            }               
            
            return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {            

            if (isOldValue(finder.getCompileName(formal)))
                return new Formal(Tok(formal.tok), this.newName, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            
            return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {         
          
            if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;


                if (isOldValue(finder.getCompileName(e.ResolvedExpression)))
                    return new ExprDotName(Tok(e.tok), CloneExpr(e.Lhs), this.newName, e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
                else
                    return base.CloneExpr(expr);
            }
            else
                return base.CloneExpr(expr);
               
        }

        public override MemberDecl CloneMember(MemberDecl member)
        {

            if (member is Field)
            {
                if (member is ConstantField)
                {
                    var c = (ConstantField)member;

                    if (isOldValue(finder.getCompileName(c)))
                        return new ConstantField(Tok(c.tok), this.newName, CloneExpr(c.constValue), c.IsGhost, CloneType(c.Type), CloneAttributes(c.Attributes));

                }
                else
                {
                    Contract.Assert(!(member is SpecialField));
                    var f = (Field)member;

                    if (isOldValue(finder.getCompileName(f)))
                        return new Field(Tok(f.tok), this.newName, f.IsGhost, f.IsMutable, f.IsUserMutable, CloneType(f.Type), CloneAttributes(f.Attributes));

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
                        
            if (isOldValue(finder.getCompileName(m)))
            {
                if (m is Constructor)
                {
                    return new Constructor(Tok(m.tok), this.newName, tps, ins,
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is InductiveLemma)
                {
                    return new InductiveLemma(Tok(m.tok), this.newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is CoLemma)
                {
                    return new CoLemma(Tok(m.tok), this.newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is Lemma)
                {
                    return new Lemma(Tok(m.tok), this.newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is TwoStateLemma)
                {
                    var two = (TwoStateLemma)m;
                    return new TwoStateLemma(Tok(m.tok), this.newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is Tactic)
                {
                    return new Tactic(Tok(m.tok), this.newName, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null);
                }
                else
                {
                    return new Method(Tok(m.tok), this.newName, m.HasStaticKeyword, m.IsGhost, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
            }
     
             return base.CloneMethod(m);
        }

    }
}
