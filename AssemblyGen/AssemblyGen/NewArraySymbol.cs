using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class NewArraySymbol : IntermediateSymbol
    {
        public NewArraySymbol(IGeneratorTarget destination, Type elementType, Symbol length) : base(destination)
        {
            _ElementType = elementType;
            _LengthExpression = Take(length, typeof(int));
        }

        private Type _ElementType;
        private IILExpressionNode _LengthExpression;

        public override Type Type => _ElementType.MakeArrayType();

        protected override IILExpressionNode ToExpressionNode()
        {
            return ILExpressionNode.NewArray(_ElementType, _LengthExpression);
        }
    }
}
