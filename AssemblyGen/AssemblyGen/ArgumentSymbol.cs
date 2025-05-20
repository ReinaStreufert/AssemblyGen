using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class ArgumentSymbol : AssignableSymbol
    {
        public override Type Type => _ParamType;

        public ArgumentSymbol(IGeneratorTarget destination, int paramIndex, Type paramType) : base(destination)
        {
            _ArgIndex = paramIndex;
            _ParamType = paramType;
        }

        private int _ArgIndex;
        private Type _ParamType;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            var closure = Destination.CurrentClosure;
            if (closure != null)
                return ILExpressionNode.LoadField(ILExpressionNode.LoadThis, closure.CaptureArgument(_ArgIndex));
            return ILExpressionNode.LoadArgument(_ArgIndex);
        }

        public override void Assign(Symbol value)
        {
            if (!value.Type.IsAssignableTo(_ParamType))
                throw new ArgumentException($"type '{value.Type.Name}' is not assignable to parameter of type '{_ParamType}'");
            var closure = Destination.CurrentClosure;
            var storeNode = closure == null ? ILExpressionNode.StoreLocal(_ArgIndex, Take(value)) :
                ILExpressionNode.StoreField(ILExpressionNode.LoadThis, closure.CaptureArgument(_ArgIndex), Take(value));
            Destination.Put(storeNode);
        }
    }
}
