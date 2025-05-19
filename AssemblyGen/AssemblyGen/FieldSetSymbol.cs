using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class FieldSetSymbol : IntermediateSymbol
    {
        public override Type Type => _Field.FieldType;

        public FieldSetSymbol(IGeneratorTarget destination) : base(destination)
        {
        }

        private IILExpressionNode? _Instance;
        private FieldInfo _Field;

        protected override IILExpressionNode ToExpressionNode()
        {
            throw new NotImplementedException();
        }
    }
}
