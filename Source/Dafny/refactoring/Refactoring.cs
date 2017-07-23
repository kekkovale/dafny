using Microsoft.Dafny.Tacny;
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
        HashSet<int> tokMap;               
        private String newName;
        private Dictionary<int, MemberDecl> updates;
        private Program program;
        private Program resolvedProgram;

        public Refactoring(Program program, Program resolvedProgram)
        {            
            this.program = program;
            updates = new Dictionary<int, MemberDecl>();
            this.resolvedProgram = resolvedProgram;
        }

        public Program FoldPredicate(int line)
        {
            ClassDecl classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            Collector collector = new Collector(program,resolvedProgram);
            Predicate predicate = collector.collectPredicate(line);


            classDecl.Members.Add(predicate);

            return program;
        }

        public Program renameRefactoring(String newName, int line, int column)
        {
            //Contract.Assert((newName != null && newName != "") && (oldName != null && oldName != ""));

            this.newName = newName;
            
            ClassDecl classDecl = program.DefaultModuleDef.TopLevelDecls.FirstOrDefault() as ClassDecl;

            Collector collector = new Collector(program,resolvedProgram);
            tokMap = collector.collectVariables(line, column);

            foreach (MemberDecl member in classDecl.Members)
            {
                MemberDecl newMember = CloneMember(member);
                int index = classDecl.Members.IndexOf(member);
                updates.Add(index, newMember);
            }

            this.updateProgram(classDecl);

            return program;
        }      

        private void updateProgram(ClassDecl classDecl)
        {

            foreach (KeyValuePair<int, MemberDecl> entry in updates)
            {
                classDecl.Members[entry.Key] = entry.Value;
                //classDecl.Members.Insert(entry.Key,entry.Value);
            }
        }



        public List<LocalVariable> cloneLocalVariables(VarDeclStmt s)
        {
            List<LocalVariable> lhss = new List<LocalVariable>();
            
            foreach (LocalVariable lv in s.Locals)
            {              
                
                if (tokMap.Contains(lv.Tok.pos))
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

            if (tokMap.Contains(nameSegment.tok.pos))
            {
                return new NameSegment(Tok(nameSegment.tok), this.newName, nameSegment.OptTypeArguments == null ? null : nameSegment.OptTypeArguments.ConvertAll(CloneType));
            }               
            
            return base.CloneNameSegment(expr);
        }

        public override Formal CloneFormal(Formal formal)
        {            

            if (tokMap.Contains(formal.tok.pos))
                return new Formal(Tok(formal.tok), this.newName, CloneType(formal.Type), formal.InParam, formal.IsGhost, formal.IsOld);
            
            return base.CloneFormal(formal);
        }

        public override Expression CloneExpr(Expression expr)
        {         
          
            if (expr is ExprDotName)
            {
                var e = (ExprDotName)expr;


                if (tokMap.Contains(e.tok.pos))
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

                    if (tokMap.Contains(c.tok.pos))
                        return new ConstantField(Tok(c.tok), this.newName, CloneExpr(c.constValue), c.IsGhost, CloneType(c.Type), CloneAttributes(c.Attributes));

                }
                else
                {
                    Contract.Assert(!(member is SpecialField));
                    var f = (Field)member;

                    if (tokMap.Contains(f.tok.pos))
                        return new Field(Tok(f.tok), this.newName, f.IsGhost, f.IsMutable, f.IsUserMutable, CloneType(f.Type), CloneAttributes(f.Attributes));

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

            if (tokMap.Contains(f.tok.pos))
            {
                if (f is Predicate)
                {
                    return new Predicate(Tok(f.tok), this.newName, f.HasStaticKeyword, f.IsProtected, f.IsGhost, tps, formals,
                      req, reads, ens, decreases, body, Predicate.BodyOriginKind.OriginalOrInherited, CloneAttributes(f.Attributes), null, f);
                }
                else if (f is InductivePredicate)
                {
                    return new InductivePredicate(Tok(f.tok), this.newName, f.HasStaticKeyword, f.IsProtected, tps, formals,
                      req, reads, ens, body, CloneAttributes(f.Attributes), null, f);
                }
                else if (f is CoPredicate)
                {
                    return new CoPredicate(Tok(f.tok), this.newName, f.HasStaticKeyword, f.IsProtected, tps, formals,
                      req, reads, ens, body, CloneAttributes(f.Attributes), null, f);
                }
                else if (f is TwoStatePredicate)
                {
                    return new TwoStatePredicate(Tok(f.tok), this.newName, f.HasStaticKeyword, tps, formals,
                      req, reads, ens, decreases, body, CloneAttributes(f.Attributes), null, f);
                }
                else if (f is TwoStateFunction)
                {
                    return new TwoStateFunction(Tok(f.tok), this.newName, f.HasStaticKeyword, tps, formals, f.Result == null ? null : CloneFormal(f.Result), CloneType(f.ResultType),
                      req, reads, ens, decreases, body, CloneAttributes(f.Attributes), null, f);
                }
                else if (f is TacticFunction)
                {
                    return new TacticFunction(Tok(f.tok), this.newName, f.HasStaticKeyword, f.IsProtected, f.IsGhost, tps, formals, CloneType(f.ResultType),
                        req, reads, ens, decreases, body, CloneAttributes(f.Attributes), null);
                }
                else
                {
                    return new Function(Tok(f.tok), this.newName, f.HasStaticKeyword, f.IsProtected, f.IsGhost, tps, formals, f.Result == null ? null : CloneFormal(f.Result), CloneType(f.ResultType),
                      req, reads, ens, decreases, body, CloneAttributes(f.Attributes), null, f);
                }
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
                        
            if (tokMap.Contains(m.tok.pos))
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
