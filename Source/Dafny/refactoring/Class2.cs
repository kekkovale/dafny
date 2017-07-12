﻿using System;
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
        private List<MemberDecl> newMembers = new List<MemberDecl>();
        private Dictionary<int, MemberDecl> updates = new Dictionary<int, MemberDecl>();
        private Program program;
        private ClassDecl classDecl;

        public Refactoring(Program program)
        {
            this.program = program;
            newMembers = new List<MemberDecl>();
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
                if (member is Method)
                {
                    Method newMethod = CloneMethod(member as Method);
                    
                    int index = classDecl.Members.IndexOf(member);
                    updates.Add(index, newMethod);                                       
                }           
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

        public override Statement CloneStmt(Statement stmt)
        {
            if (stmt is VarDeclStmt)
            {
                var s = (VarDeclStmt)stmt;
                var lhss = s.Locals.ConvertAll(c => new LocalVariable(Tok(c.Tok), Tok(c.EndTok), this.newValue, CloneType(c.OptionalType), c.IsGhost));
                return new VarDeclStmt(Tok(s.Tok), Tok(s.EndTok), lhss, (ConcreteUpdateStatement)CloneStmt(s.Update));
            }
            else
                return base.CloneStmt(stmt);
        }

        public override Expression CloneNameSegment(Expression expr)
        {
            var nameSegment = expr as NameSegment;
            if(nameSegment.Name == this.oldValue)
            {
                return new NameSegment(Tok(nameSegment.tok), this.newValue, nameSegment.OptTypeArguments == null ? null : nameSegment.OptTypeArguments.ConvertAll(CloneType));
            }else
                return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {
            if (formal.DisplayName == this.oldValue)
                return new Formal(Tok(formal.tok), this.newValue, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            else
                return base.CloneFormal(formal);
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
