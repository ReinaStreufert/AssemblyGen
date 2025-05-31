using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class FieldGetSymbol : IntermediateSymbol
    {
        public static FieldGetSymbol Create(IGeneratorTarget destination, Symbol? instance, FieldInfo field)
        {
            var instanceNode = instance == null ? null : Take(instance, field.DeclaringType!);
            return new FieldGetSymbol(destination, instanceNode, field);
        }

        public override Type Type => _Field.FieldType;

        private FieldGetSymbol(IGeneratorTarget destination, IILExpressionNode? instance, FieldInfo field) : base(destination)
        {
            _Instance = instance;
            _Field = field;
        }

        private IILExpressionNode? _Instance;
        private FieldInfo _Field;

        protected override IILExpressionNode ToExpressionNode()
        {
            return ILExpressionNode.LoadField(_Instance, _Field);
        }
    }
}
