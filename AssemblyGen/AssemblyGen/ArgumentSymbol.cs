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

        public ArgumentSymbol(IGeneratorTarget destination, int paramIndex, Type paramType, int containerClosureLevel) : base(destination)
        {
            _ArgIndex = paramIndex;
            _ParamType = paramType;
            _ContainerClosureLevel = containerClosureLevel;
        }

        private int _ArgIndex;
        private int _ContainerClosureLevel;
        private Type _ParamType;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            var closure = Destination.CurrentClosure;
            if (closure != null && closure.Level >= _ContainerClosureLevel)
                return ILExpressionNode.LoadField(ILExpressionNode.LoadThis, closure.CaptureArgument(_ArgIndex, _ContainerClosureLevel, _ParamType));
            return ILExpressionNode.LoadArgument(_ArgIndex);
        }

        public override void Assign(Symbol value)
        {
            if (!value.Type.IsAssignableTo(Type))
                throw new ArgumentException($"Type '{value.Type.Name}' is not assignable to local of type '{Type.Name}'");
            var closure = Destination.CurrentClosure;
            ILNode storeNode = closure != null && closure.Level >= _ContainerClosureLevel ?
                ILExpressionNode.StoreField(ILExpressionNode.LoadThis, closure.CaptureArgument(_ArgIndex, _ContainerClosureLevel, _ParamType), Take(value)) :
                ILExpressionNode.StoreArgument(_ArgIndex, Take(value));
            Destination.Put(storeNode);
        }
    }
}
