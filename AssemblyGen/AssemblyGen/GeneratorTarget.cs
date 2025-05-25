using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public partial class MethodGeneratorContext
    {
        protected class GeneratorTarget : IGeneratorTarget
        {
            public IClosure? CurrentClosure { get; set; }

            public GeneratorTarget(MethodGeneratorContext ctx)
            {
                _Ctx = ctx;
            }

            private MethodGeneratorContext _Ctx;

            public IStatement Put(IILExpressionNode node)
            {
                var statement = new Statement(node);
                _Ctx._EmitList.Add(statement);
                return statement;
            }

            public void Put(IEnumerable<IEmittable> emittables)
            {
                _Ctx._EmitList.AddRange(emittables);
            }

            private class Statement : IStatement, IEmittable
            {
                public Statement(IILExpressionNode node)
                {
                    _Node = node;
                }

                private bool _Withdrawn = false;
                private IILExpressionNode _Node;

                public void Withdraw()
                {
                    _Withdrawn = true;
                }

                public void Emit(ILGenerator il)
                {
                    if (!_Withdrawn)
                        _Node.WriteInstructions(il);
                }
            }
        }
    }
}
