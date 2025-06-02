using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class UnaryOperationSymbol : IntermediateSymbol
    {
        public static UnaryOperationSymbol Create(IGeneratorTarget destination, UnaryOperator unaryOp, Symbol operand)
        {
            var mapping = _Mappings
                .Where(m => m.IsMatch(unaryOp, operand.Type))
                .FirstOrDefault();
            if (mapping == null)
                throw new ArgumentException($"{nameof(unaryOp)} does not match {nameof(operand)}");
            return new UnaryOperationSymbol(destination, mapping.OpNodeFunc(Take(operand, operand.Type)), mapping.ResultType ?? operand.Type);
        }

        private static readonly UnaryOperatorMapping[] _Mappings = new UnaryOperatorMapping[]
        {
            new(UnaryOperator.Not, typeof(bool), static operand => ILExpressionNode.Equal(operand, ILExpressionNode.Constant(false)), typeof(bool)),
            new(UnaryOperator.BitwiseNot, null, ILExpressionNode.Compliment, typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long))
        };

        public override Type Type => _ResultType;

        private UnaryOperationSymbol(IGeneratorTarget destination, IILExpressionNode operationNode, Type resultType) : base(destination)
        {
            _OperationNode = operationNode;
            _ResultType = resultType;
        }

        private IILExpressionNode _OperationNode;
        private Type _ResultType;

        protected override IILExpressionNode ToExpressionNode()
        {
            return _OperationNode;
        }

        private class UnaryOperatorMapping
        {
            public UnaryOperator Operator { get; }
            public Type[] AcceptableOperandTypes { get; }
            public Func<IILExpressionNode, IILExpressionNode> OpNodeFunc { get; }
            public Type? ResultType { get; } // null=result same as operand
            
            public UnaryOperatorMapping(UnaryOperator unaryOp, Type? resultType, Func<IILExpressionNode, IILExpressionNode> opNodeFunc, params Type[] acceptableOperandTypes)
            {
                Operator = unaryOp;
                AcceptableOperandTypes = acceptableOperandTypes;
                OpNodeFunc = opNodeFunc;
                ResultType = resultType;
            }

            public bool IsMatch(UnaryOperator op, Type operandType)
            {
                return op == Operator && AcceptableOperandTypes.Contains(operandType);
            }
        }
    }
}
