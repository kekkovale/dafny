using System;
using Microsoft.Boogie;

namespace Microsoft.Dafny.Tacny {
    // Static as we need to "global" consistency...
    static class TokenGenerator {

        private static int _idx = -1;
        private static Func<int, Tuple<IToken, IToken>> _tokens = _ => null;

        public static IToken NextToken(IToken start = null, IToken end = null) {
            IToken ntok = new Microsoft.Boogie.Token(_idx,_idx);
            if (start != null & end != null) {
                var tup = new Tuple<IToken, IToken>(start, end);
                _tokens = x => x == _idx ? tup : _tokens(x);
            }
            _idx--;
            return ntok;
        }

        public static Tuple<IToken, IToken> ActualTokens(int idx) {
            return _tokens(idx);
        }

    }
}
