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

        public LambdaDelegateSymbol(IGeneratorTarget destination, IILExpressionNode delegatePtr, Type delegateType) : base(destination)
        {
            _DelegatePtr = delegatePtr;
            _DelegateConstructor = delegateType.GetConstructors()
                .Where(c => c.GetParameters().Length == 1)
                .First();
        }

        private IILExpressionNode _DelegatePtr;
        private ConstructorInfo _DelegateConstructor;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            return ILExpressionNode.NewObject(_DelegateConstructor, _DelegatePtr);
        }
    }
}
