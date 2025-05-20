using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class LocalSymbol : AssignableSymbol
    {
        public override Type Type => _LocalBuilder.LocalType;

        public LocalSymbol(IGeneratorTarget destination, LocalBuilder builder, int closureLevel = 0) : base(destination)
        {
            _LocalBuilder = builder;
            _ContainerClosureLevel = closureLevel;
        }

        private LocalBuilder _LocalBuilder;
        private int _ContainerClosureLevel;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            var closure = Destination.CurrentClosure;
            if (closure != null && closure.Level >= _ContainerClosureLevel)
                return ILExpressionNode.LoadField(ILExpressionNode.LoadThis, closure.CaptureLocal(_LocalBuilder.LocalIndex, _ContainerClosureLevel, Type));
            return ILExpressionNode.LoadLocal(_LocalBuilder.LocalIndex);
        }

        public override void Assign(Symbol value)
        {
            if (!value.Type.IsAssignableTo(Type))
                throw new ArgumentException($"Type '{value.Type.Name}' is not assignable to local of type '{Type.Name}'");
            var closure = Destination.CurrentClosure;
            ILNode storeNode = closure != null && closure.Level >= _ContainerClosureLevel ? 
                ILExpressionNode.StoreField(ILExpressionNode.LoadThis, closure.CaptureLocal(_LocalBuilder.LocalIndex, _ContainerClosureLevel, Type), Take(value)) :
                ILExpressionNode.StoreLocal(_LocalBuilder.LocalIndex, Take(value));
            Destination.Put(storeNode);
        }
    }
}
