using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class MethodCallSymbol : IntermediateSymbol
    {
        public static MethodCallSymbol Create(IGeneratorTarget destination, MethodInfo method, Symbol? instance, params Symbol[] arguments)
        {
            var instanceNode = instance != null ? Take(instance) : null;
            var argumentNodes = arguments
                .Select(Take)
                .ToArray();
            var callNode = ILExpressionNode.Call(instanceNode, method, false, argumentNodes);
            var callStatement = destination.Put(callNode);
            return new MethodCallSymbol(destination, method.ReturnType, callStatement, callNode);
        }

        public override Type Type => _ReturnType;

        private MethodCallSymbol(IGeneratorTarget destination, Type returnType, IStatement callStatement, ILMethodCallNode callNode) : base(destination)
        {
            _ReturnType = returnType;
            _CallStatement = callStatement;
            _CallNode = callNode;
        }

        private Type _ReturnType;
        private IStatement _CallStatement;
        private ILMethodCallNode _CallNode;

        protected override IILExpressionNode ToExpressionNode()
        {
            if (_ReturnType == typeof(void))
                throw new InvalidOperationException($"The method does not return a value");
            _CallStatement.Withdraw();
            _CallNode.PreserveReturnValue = true;
            return _CallNode;
        }
    }
}
