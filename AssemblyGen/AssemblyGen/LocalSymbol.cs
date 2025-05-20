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

        public LocalSymbol(IGeneratorTarget destination, LocalBuilder builder) : base(destination)
        {
            _LocalBuilder = builder;
        }

        private LocalBuilder _LocalBuilder;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            var closure = Destination.CurrentClosure;
            if (closure != null)
                return ILExpressionNode.LoadField(ILExpressionNode.LoadThis, closure.CaptureLocal(_LocalBuilder.LocalIndex));
            return ILExpressionNode.LoadLocal(_LocalBuilder.LocalIndex);
        }

        public override void Assign(Symbol value)
        {
            if (!value.Type.IsAssignableTo(Type))
                throw new ArgumentException($"Type '{value.Type.Name}' is not assignable to local of type '{Type.Name}'");
            var closure = Destination.CurrentClosure;
            var storeNode = closure == null ? ILExpressionNode.StoreLocal(_LocalBuilder.LocalIndex, Take(value)) :
                ILExpressionNode.StoreField(ILExpressionNode.LoadThis, closure.CaptureLocal(_LocalBuilder.LocalIndex), Take(value));
            Destination.Put(storeNode);
        }
    }
}
