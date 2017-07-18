using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Dafny.refactoring
{
    class FindExpression : Refactoring
    {
        public FindExpression(Program program) : base(program)
        {
        }

        public override Statement CloneStmt(Statement stmt)
        {

            return base.CloneStmt(stmt);
        }
    }
}
