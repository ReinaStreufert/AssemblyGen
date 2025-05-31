using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class TypeOfSymbol : IntermediateSymbol
    {
        public override Type Type => typeof(Type);

        public TypeOfSymbol(IGeneratorTarget destination, Type type) : base(destination)
        {
            _Type = type;
        }

        private Type _Type;

        protected override IILExpressionNode ToExpressionNode()
        {
            var getTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;
            return ILExpressionNode.Call(null, getTypeFromHandleMethod, true, ILExpressionNode.LoadToken(_Type.MetadataToken));
        }
    }
}
