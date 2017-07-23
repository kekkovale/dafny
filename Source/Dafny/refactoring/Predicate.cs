using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.refactoring
{
    class myPredicate
    {
        //public Boogie.IToken tok;
        private List<TypeParameter> typeArgs;
        private List<Formal> formals;
        private List<Expression> req;
        private List<FrameExpression> reads;
        private List<Expression> ens;
        private Specification<Expression> decreases;
        private Expression body;
        private Attributes attributes;
        private List<Expression> args;

        public myPredicate()
        {
            typeArgs = new List<TypeParameter>();
            formals = new List<Formal>();
            req = new List<Expression>();
            reads = new List<FrameExpression>();
            ens = new List<Expression>();
            args = new List<Expression>();
        }

        public List<TypeParameter> TypeArgs
        {
            get
            {
                return typeArgs;
            }

            set
            {
                typeArgs = value;
            }
        }

        public List<Formal> Formals
        {
            get
            {
                return formals;
            }

            set
            {
                formals = value;
            }
        }

        public List<Expression> Req
        {
            get
            {
                return req;
            }

            set
            {
                req = value;
            }
        }

        public List<FrameExpression> Reads
        {
            get
            {
                return reads;
            }

            set
            {
                reads = value;
            }
        }

        public List<Expression> Ens
        {
            get
            {
                return ens;
            }

            set
            {
                ens = value;
            }
        }

        public Specification<Expression> Decreases
        {
            get
            {
                return decreases;
            }

            set
            {
                decreases = value;
            }
        }

        public Expression Body
        {
            get
            {
                return body;
            }

            set
            {
                body = value;
            }
        }

        public Attributes Attributes
        {
            get
            {
                return attributes;
            }

            set
            {
                attributes = value;
            }
        }

        public List<Expression> Args
        {
            get
            {
                return args;
            }

            set
            {
                args = value;
            }
        }
    }
}
