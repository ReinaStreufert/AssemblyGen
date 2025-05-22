using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class LambdaDelegateSymbol : Symbol
    {
        public override Type Type => _DelegateConstructor.DeclaringType!;

        public LambdaDelegateSymbol(IGeneratorTarget destination, IILExpressionNode closureInst, IILExpressionNode invocationMethodPtr, Type delegateType) : base(destination)
        {
            _ClosureInst = closureInst;
            _InvocationMethodPtr = invocationMethodPtr;
            _DelegateConstructor = delegateType.GetConstructors()
                .Where(c => c.GetParameters().Length == 2)
                .First();
        }

        private IILExpressionNode _ClosureInst;
        private IILExpressionNode _InvocationMethodPtr;
        private ConstructorInfo _DelegateConstructor;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            return ILExpressionNode.NewObject(_DelegateConstructor, _ClosureInst, _InvocationMethodPtr);
        }
    }
}
