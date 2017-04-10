/*
 * Implementation of Huet's Zipper: work in progress... 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using ExistsExpr = Microsoft.Dafny.ExistsExpr;
using ForallExpr = Microsoft.Dafny.ForallExpr;
using Formal = Microsoft.Dafny.Formal;
using LiteralExpr = Microsoft.Dafny.LiteralExpr;
using QuantifierExpr = Microsoft.Dafny.QuantifierExpr;

namespace Microsoft.Dafny.Tacny.Expr {

    // won't work if an expression has two list of expressions!
    class Zipper {

        private class Path {
            public Path Parent;
            public Expression Exp;
            public int Cur = 0; // Each type needs to keep track of what this means
        }

        private Path _path;
        private Expression _exp;

        public Expression Exp => _exp;

      public Zipper(Expression e) {
            _exp = e;
            _path = null; // replace with option type?
        }

        public bool GoLeft() {
            if (_path != null && (_path != null || _path.Cur == 0))
                return false;
            else {
              if (_path != null)
              {
                _path.Cur--;
                _exp = _path.Exp.SubExpressions.ElementAt(_path.Cur);
              }
              return true;
            }
        }

        public bool GoRight() {
            if (_path != null && (_path != null || _path.Cur < _path.Exp.SubExpressions.Count() - 1))
                return false;
            else {
              if (_path != null)
              {
                _path.Cur++;
                _exp = _path.Exp.SubExpressions.ElementAt(_path.Cur);
              }
              return true;
            }
            return false;
        }

        public bool GoDown() {
            Expression e = _exp.SubExpressions.First();
            if (e == null)
                return false;
            Path p = new Path(){Parent = _path, Exp = _exp};
            _path = p;
            _exp = e;
            return true;
        }

        // TODO: do we need to fix the counter?
        // TODO: what about changes, they should be okay unless all is readonly?
        public bool GoUp() {
            if (_path == null)
                return false;
            _exp = _path.Exp;
            _path = _path.Parent;
            return true;
        }


        // DERIVED:
        public void GoFirst() {
            while (GoUp()) {}
        }

        public void GoLeftMost() {
            while (GoLeft()) { }
        }
        public void GoRightMost() {
            while (GoRight()) { }
        }

        public void GoLast() {
            GoRightMost();
            while (GoDown())
                GoRightMost();
        }

        // depth-first
        public bool GoNext() {
            if (GoDown())
                return true;
            if (GoRight())
                return true;
            if (GoRight())
                return true;
            // We then have to keep on going up and right until we reach the top (meaning all have been seen)
            while (GoUp()) {
                if (GoRight())
                    return true;
            }
            // Go to the last and return false -- could be skipped
            //GoLast();
            return false;
        }

        public bool GoTo(Predicate<Expression> p) {
            if (p(_exp))
                return true;
            if (!GoNext())
                return false;
            return GoTo(p);
        }

        public T Fold<T>(Func<Expression,T,T> f,T init) {
            GoFirst();
            T res = f(_exp,init);
            while (GoNext()) {
                res = f(_exp, res);
            }
            return res;
        }

        public IEnumerator<T> Map<T>(Func<Expression, T> f) {
            GoFirst();
            yield return f(_exp);
            while (GoNext()) {
                yield return f(_exp);
            }
        }

        // has side effect
        public Expression GoFirstandReturn() {
            GoFirst();
            return _exp;
        }

        // no side-effect (but not sure about updates!)
        public Expression ReturnFirst() {
            if (_path == null)
                return _exp;
            Path p = _path;
            while (p.Parent != null)
                p = p.Parent;
            return p.Exp;
        }

        // Mutable operations: this is likely to depend on type of operations, and probably has to be pushed upwards
        public bool Update(Func<Expression,Expression> f) {
            Expression e = f(_exp);
            // Need to call parent to update pointer for current to e!
            return false;
        }

        


    }

    
 
}
