using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class CastSymbol : IntermediateSymbol
    {
        public CastSymbol(IGeneratorTarget destination, Type type, Symbol value) : base(destination)
        {
            _Type = type;
            _Expression = Symbol.Take(value, typeof(object));
        }

        private IILExpressionNode _Expression;
        private Type _Type;

        public override Type Type => _Type;

        protected override IILExpressionNode ToExpressionNode()
        {
            return ILExpressionNode.CastClass(_Type, _Expression);
        }
    }
}
