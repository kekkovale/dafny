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

        private String oldValue;
        private String newValue;
        private Dictionary<int, MemberDecl> updates;
        private Program program;
        private ClassDecl classDecl;

        public Refactoring(Program program)
        {
            this.program = program;
            updates = new Dictionary<int, MemberDecl>();
            classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
        }

        public Program renameRefactoring(String oldName, String newName)
        {
            Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));

            this.newValue = newName;
            this.oldValue = oldName;

            foreach (MemberDecl member in classDecl.Members)
            {       
                
                MemberDecl newMember = CloneMember(member);
                    
                int index = classDecl.Members.IndexOf(member);
                updates.Add(index, newMember);                                       
       
            }
            this.update(classDecl);   

            return program;
        }

        public void update(ClassDecl classDecl)
        {
            foreach (KeyValuePair<int, MemberDecl> entry in updates)
            {
                classDecl.Members.RemoveAt(entry.Key);
                classDecl.Members.Insert(entry.Key,entry.Value);
            }
        }

        public bool isOldValue(String oldValue)
        {
            if (this.oldValue == oldValue)
                return true;
            else
                return false;
        }

        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();

            foreach (LocalVariable lv in s.Locals)
            {
                if (isOldValue(lv.DisplayName))
                {
                    var newLv = new LocalVariable(Tok(lv.Tok), Tok(lv.EndTok), this.newValue, CloneType(lv.OptionalType), lv.IsGhost);
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
            if(isOldValue(nameSegment.Name))
            {
                return new NameSegment(Tok(nameSegment.tok), this.newValue, nameSegment.OptTypeArguments == null ? null : nameSegment.OptTypeArguments.ConvertAll(CloneType));
            }else
                return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {
            if (isOldValue(formal.DisplayName))
                return new Formal(Tok(formal.tok), this.newValue, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            else
                return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {
            if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;

                if(isOldValue(e.SuffixName))
                    return new ExprDotName(Tok(e.tok), CloneExpr(e.Lhs), this.newValue, e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
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

                    if (isOldValue(c.Name))
                        return new ConstantField(Tok(c.tok), this.newValue, CloneExpr(c.constValue), c.IsGhost, CloneType(c.Type), CloneAttributes(c.Attributes));
                    else
                        return base.CloneMember(member);
                }
                else
                {
                    Contract.Assert(!(member is SpecialField));
                    var f = (Field)member;

                    if(isOldValue(f.Name))
                        return new Field(Tok(f.tok), this.newValue, f.IsGhost, f.IsMutable, f.IsUserMutable, CloneType(f.Type), CloneAttributes(f.Attributes));
                    else
                        return base.CloneMember(member);
                }
            }
            else
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

            if (m.Name == this.oldValue)
            {
                if (m is Constructor)
                {
                    return new Constructor(Tok(m.tok), this.newValue, tps, ins,
                      req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is InductiveLemma)
                {
                    return new InductiveLemma(Tok(m.tok), this.newValue, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                      req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is CoLemma)
                {
                    return new CoLemma(Tok(m.tok), this.newValue, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                      req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is Lemma)
                {
                    return new Lemma(Tok(m.tok), this.newValue, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                      req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is TwoStateLemma)
                {
                    var two = (TwoStateLemma)m;
                    return new TwoStateLemma(Tok(m.tok), this.newValue, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                      req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
                else if (m is Tactic)
                {
                    return new Tactic(Tok(m.tok), this.newValue, m.HasStaticKeyword, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null);
                }
                else
                {
                    return new Method(Tok(m.tok), this.newValue, m.HasStaticKeyword, m.IsGhost, tps, ins, m.Outs.ConvertAll(CloneFormal),
                        req, mod, ens, decreases, body, CloneAttributes(m.Attributes), null, m);
                }
            }
            else
                return base.CloneMethod(m);
        }

    }
}
