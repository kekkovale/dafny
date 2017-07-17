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
        private int line;
        private int column;
        private Dictionary<int, MemberDecl> updates;
        private Program program;
        private ClassDecl classDecl;
        private bool finding;
        private bool exprFound;
        private String currentMethod;

        public Refactoring(Program program)
        {
            this.program = program;
            updates = new Dictionary<int, MemberDecl>();
            classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;
            finding = false;
        }

        public Program renameRefactoring(String newName, int line, int column)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));

            this.newValue = newName;
            this.line = line;
            this.column = column;

            if (findExpression())
            {
                foreach (MemberDecl member in classDecl.Members)
                {

                    MemberDecl newMember = CloneMember(member);

                    int index = classDecl.Members.IndexOf(member);
                    updates.Add(index, newMember);

                }
                this.update(classDecl);
            }

            return program;
        }

        public bool findExpression()
        {
            
            this.finding = true;
            foreach (MemberDecl member in classDecl.Members)
            {
                CloneMember(member);
            }

            this.finding = false;

            if (this.oldValue != null)
                return this.exprFound = true;
            else
                return this.exprFound = false;
        }

        public String getCompileName<T>(T expr)
        {
            String compileName = null;

            if(expr is IdentifierExpr)
            {
                IdentifierExpr matchExpr = expr as IdentifierExpr;
                compileName = matchExpr.Var.CompileName;
                                
            }
            else if(expr is Formal)
            {
                Formal matchExpr = expr as Formal;
                compileName = matchExpr.CompileName;
            }
            else if (expr is LocalVariable)
            {
                LocalVariable matchExpr = expr as LocalVariable;
                compileName = matchExpr.CompileName;
            }
            else if(expr is MemberSelectExpr)
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

       

        public void update(ClassDecl classDecl)
        {

            foreach (KeyValuePair<int, MemberDecl> entry in updates)
            {
                classDecl.Members[entry.Key] = entry.Value;
                //classDecl.Members.Insert(entry.Key,entry.Value);
            }
        }

        public bool isOldValue(String oldValue)
        {
            return this.oldValue == oldValue;
        }

        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();
            
            foreach (LocalVariable lv in s.Locals)
            {
                if (finding)
                {
                    if (lv.Tok.line == this.line && lv.Tok.col == this.column)
                    {
                        this.oldValue = this.getCompileName(lv);
                        
                    }
                }
                else
                {
                    if (isOldValue(lv.CompileName))
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
            
            if (finding)
            {
                if (nameSegment.tok.line == this.line && nameSegment.tok.col == this.column)
                {
                    this.oldValue = this.getCompileName(nameSegment.ResolvedExpression);
                }                
            }
            else
            {
                
                if (isOldValue(this.getCompileName(nameSegment.ResolvedExpression)))
                {
                    return new NameSegment(Tok(nameSegment.tok), this.newValue, nameSegment.OptTypeArguments == null ? null : nameSegment.OptTypeArguments.ConvertAll(CloneType));
                }               
            }
            return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {
            if (finding)
            {
                if (formal.tok.line == this.line && formal.tok.col == this.column)
                    this.oldValue = this.getCompileName(formal);

            } else {

                if (isOldValue(this.getCompileName(formal)))
                    return new Formal(Tok(formal.tok), this.newValue, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            }

            return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {
            if (finding)
            {
                if (expr is ExprDotName)
                {
                    var e = (ExprDotName)expr;


                    if (e.tok.line == this.line && e.tok.col == this.column)
                        this.oldValue = this.getCompileName(e.ResolvedExpression);
                }
                return base.CloneExpr(expr);
            }
            else
            {
                if (expr is ExprDotName)
                {
                    var e = (ExprDotName)expr;


                    if (isOldValue(this.getCompileName(e.ResolvedExpression)))
                        return new ExprDotName(Tok(e.tok), CloneExpr(e.Lhs), this.newValue, e.OptTypeArguments == null ? null : e.OptTypeArguments.ConvertAll(CloneType));
                    else
                        return base.CloneExpr(expr);
                }
                else
                    return base.CloneExpr(expr);
            }
            
        }

        public override MemberDecl CloneMember(MemberDecl member)
        {
            if (finding)
            {
                if (member is Field)
                {
                    if (member is ConstantField)
                    {
                        var c = (ConstantField)member;

                        if (c.tok.line == this.line && c.tok.col == this.column)
                            this.oldValue = this.getCompileName(c);
                       
                    }
                    else
                    {
                        Contract.Assert(!(member is SpecialField));
                        var f = (Field)member;

                        if (f.tok.line == this.line && f.tok.col == this.column)
                            this.oldValue = this.getCompileName(f);
                        
                    }
                }
            }
            else
            {
                if (member is Field)
                {
                    if (member is ConstantField)
                    {
                        var c = (ConstantField)member;

                        if (isOldValue(this.getCompileName(c)))
                            return new ConstantField(Tok(c.tok), this.newValue, CloneExpr(c.constValue), c.IsGhost, CloneType(c.Type), CloneAttributes(c.Attributes));

                    }
                    else
                    {
                        Contract.Assert(!(member is SpecialField));
                        var f = (Field)member;

                        if (isOldValue(this.getCompileName(f)))
                            return new Field(Tok(f.tok), this.newValue, f.IsGhost, f.IsMutable, f.IsUserMutable, CloneType(f.Type), CloneAttributes(f.Attributes));

                    }
                }
            }
           
            return base.CloneMember(member);

        }

        
        public override Method CloneMethod(Method m)
        {
            this.currentMethod = this.getCompileName(m);


            var tps = m.TypeArgs.ConvertAll(CloneTypeParam);
            var ins = m.Ins.ConvertAll(CloneFormal);
            var req = m.Req.ConvertAll(CloneMayBeFreeExpr);
            var mod = CloneSpecFrameExpr(m.Mod);
            var decreases = CloneSpecExpr(m.Decreases);

            var ens = m.Ens.ConvertAll(CloneMayBeFreeExpr);

            BlockStmt body = CloneMethodBody(m);

            if (finding)
            {
                if (m.tok.line == this.line && m.tok.col == this.column)
                    this.oldValue = this.getCompileName(m);
            }
            else
            {
                if (isOldValue(this.getCompileName(m)))
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
            }
                    
             return base.CloneMethod(m);
        }

    }
}
