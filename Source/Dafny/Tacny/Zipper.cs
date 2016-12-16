/*
 * Implementation of Huet's Zipper 
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Dafny;
using System.Numerics;
using Microsoft.Boogie;
using ExistsExpr = Microsoft.Dafny.ExistsExpr;
using ForallExpr = Microsoft.Dafny.ForallExpr;
using Formal = Microsoft.Dafny.Formal;
using LiteralExpr = Microsoft.Dafny.LiteralExpr;
using QuantifierExpr = Microsoft.Dafny.QuantifierExpr;

namespace Microsoft.Dafny.Tacny {


    class Path {
        public List<Expression> Left;
        public Path Focus;
        public List<Expression> Right;
    }

    class Zipper {
        private Expression _exp { get; set; }
        private Path _path;

        public Zipper(Expression e) {
            _exp = e;
            _path = null; // replace with option type?
        }

        // return false if failing
        public bool GoLeft() {
            return false;
        }

        public bool GoRight() {
            return false;
        }

    }
}
