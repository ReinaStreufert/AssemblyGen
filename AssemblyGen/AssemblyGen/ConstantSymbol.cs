using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class ConstantSymbol : Symbol
    {
        public override Type Type => _ConstType;

        public ConstantSymbol(IGeneratorTarget destination, object? value) : base(destination)
        {
            _ConstNode = ILExpressionNode.Constant(value);
            _ConstType = value == null ? typeof(object) : value.GetType();
        }

        private IILExpressionNode _ConstNode;
        private Type _ConstType;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            return _ConstNode;
        }
    }
}
