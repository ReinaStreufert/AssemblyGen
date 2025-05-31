using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class ConstructorCallSymbol : IntermediateSymbol
    {
        public override Type Type => _Constructor.DeclaringType!;

        public ConstructorCallSymbol(IGeneratorTarget destination, ConstructorInfo constructor, params Symbol[] argumentSymbols) : base(destination)
        {
            var constructorParams = constructor.GetParameters();
            _Constructor = constructor;
            _Arguments = argumentSymbols
                .Select((s, i) => Take(argumentSymbols[i], constructorParams[i].ParameterType))
                .ToArray();
            var parameters = constructor.GetParameters();
        }

        private ConstructorInfo _Constructor;
        private IILExpressionNode[] _Arguments;

        protected override IILExpressionNode ToExpressionNode()
        {
            return ILExpressionNode.NewObject(_Constructor, _Arguments);
        }
    }
}
