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
        public override Type Type => _Field.FieldType;

        public FieldGetSymbol(IGeneratorTarget destination, Symbol? instance, FieldInfo field) : base(destination)
        {
            if (instance != null)
                _Instance = Take(instance);
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
