using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class DelegateSymbol : Symbol
    {
        public override Type Type => _DelegateConstructor.DeclaringType!;

        public DelegateSymbol(IGeneratorTarget destination, IILExpressionNode? inst, MethodInfo invocationMethod, Type delegateType) : base(destination)
        {
            _Inst = inst ?? ILExpressionNode.Constant(null);
            _InvocationMethod = invocationMethod;
            _DelegateConstructor = delegateType.GetConstructors()
                .Where(c => c.GetParameters().Length == 2)
                .First();
        }

        private IILExpressionNode _Inst;
        private MethodInfo _InvocationMethod;
        private ConstructorInfo _DelegateConstructor;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            return ILExpressionNode.NewObject(_DelegateConstructor, _Inst, ILExpressionNode.LoadFunction(_InvocationMethod));
        }
    }
}
