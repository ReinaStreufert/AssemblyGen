using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class BinaryOperationSymbol : Symbol
    {
        public static BinaryOperationSymbol Create(IGeneratorTarget destination, BinaryOperator binaryOp, Symbol operandA, Symbol operandB)
        {
            var mapping = _Mappings
                .Where(m => m.IsMatch(binaryOp, operandA.Type, operandB.Type))
                .FirstOrDefault();
            if (mapping == null)
                throw new ArgumentException($"{nameof(binaryOp)} does not match operand types");
            var a = Take(operandA, operandA.Type);
            var b = Take(operandB, operandB.Type);
            return new BinaryOperationSymbol(destination, mapping.OpNodeFunc(a, b), mapping.ResultType);
        }

        private static readonly BinaryOperatorMapping[] _Mappings = new BinaryOperatorMapping[]
        {
            new(BinaryOperator.LessThan, typeof(bool), (a, b) => ILExpressionNode.LessThan(a, b, true), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong)),
            new(BinaryOperator.LessThan, typeof(bool), (a, b) => ILExpressionNode.LessThan(a, b, false), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)),
            new(BinaryOperator.LessThan, typeof(bool), (a, b) => ILExpressionNode.LessThan(a, b, false), typeof(decimal), typeof(float), typeof(double)),
            new(BinaryOperator.GreaterThan, typeof(bool), (a, b) => ILExpressionNode.GreaterThan(a, b, true), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong)),
            new(BinaryOperator.GreaterThan, typeof(bool), (a, b) => ILExpressionNode.GreaterThan(a, b, false), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)),
            new(BinaryOperator.GreaterThan, typeof(bool), (a, b) => ILExpressionNode.GreaterThan(a, b, false), typeof(decimal), typeof(float), typeof(double)),
            new(BinaryOperator.EqualTo, typeof(bool), ILExpressionNode.Equal),
            new(BinaryOperator.And, typeof(bool), ILExpressionNode.And, typeof(bool)),
            new(BinaryOperator.Or, typeof(bool), ILExpressionNode.Or, typeof(bool)),
        }
        .Concat(ArithmeticMappings(BinaryOperator.Plus, ILExpressionNode.Add))
        .Concat(ArithmeticMappings(BinaryOperator.Minus, ILExpressionNode.Subtract))
        .Concat(ArithmeticMappings(BinaryOperator.Times, ILExpressionNode.Multiply))
        .Concat(ArithmeticMappings(BinaryOperator.Divide, ILExpressionNode.Divide))
        .Concat(BitwiseMappings(BinaryOperator.BitwiseAnd, ILExpressionNode.And))
        .Concat(BitwiseMappings(BinaryOperator.BitwiseOr, ILExpressionNode.Or))
        .Concat(BitwiseMappings(BinaryOperator.BitwiseXor, ILExpressionNode.Xor))
        .ToArray();

        private static IEnumerable<BinaryOperatorMapping> ArithmeticMappings(BinaryOperator binaryOp, Func<IILExpressionNode, IILExpressionNode, bool, IILExpressionNode> opNodeFunc)
        {
            yield return new(binaryOp, typeof(uint), (a, b) => opNodeFunc(a, b, true), typeof(byte), typeof(ushort), typeof(uint));
            yield return new(binaryOp, typeof(ulong), (a, b) => opNodeFunc(a, b, true), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong));
            yield return new(binaryOp, typeof(int), (a, b) => opNodeFunc(a, b, false), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint));
            yield return new(binaryOp, typeof(long), (a, b) => opNodeFunc(a, b, false), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong));
            yield return new(binaryOp, typeof(float), (a, b) => opNodeFunc(a, b, false), typeof(decimal), typeof(float));
            yield return new(binaryOp, typeof(double), (a, b) => opNodeFunc(a, b, false), typeof(decimal), typeof(float), typeof(double));
        }

        private static IEnumerable<BinaryOperatorMapping> BitwiseMappings(BinaryOperator binaryOp, Func<IILExpressionNode, IILExpressionNode, IILExpressionNode> opNodeFunc)
        {
            yield return new(binaryOp, typeof(uint), opNodeFunc, typeof(byte), typeof(ushort), typeof(uint));
            yield return new(binaryOp, typeof(ulong), opNodeFunc, typeof(byte), typeof(ushort), typeof(uint), typeof(ulong));
            yield return new(binaryOp, typeof(int), opNodeFunc, typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint));
            yield return new(binaryOp, typeof(long), opNodeFunc, typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong));
        }

        public override Type Type => _ResultType;

        private BinaryOperationSymbol(IGeneratorTarget destination, IILExpressionNode operationNode, Type resultType) : base(destination)
        {
            _OperationNode = operationNode;
            _ResultType = resultType;
        }

        private IILExpressionNode _OperationNode;
        private Type _ResultType;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            return _OperationNode;
        }

        private class BinaryOperatorMapping
        {
            public BinaryOperator Operator { get; }
            public Type[] AcceptableOperandTypes { get; }
            public Func<IILExpressionNode, IILExpressionNode, IILExpressionNode> OpNodeFunc { get; }
            public Type ResultType { get; }

            public BinaryOperatorMapping(BinaryOperator binaryOp, Type resultType, Func<IILExpressionNode, IILExpressionNode, IILExpressionNode> opNodeFunc, params Type[] acceptableOperandTypes)
            {
                Operator = binaryOp;
                OpNodeFunc = opNodeFunc;
                AcceptableOperandTypes = acceptableOperandTypes;
                ResultType = resultType;
            }

            public bool IsMatch(BinaryOperator binaryOp, Type operandAType, Type operandBType)
            {
                return Operator == binaryOp &&
                    (AcceptableOperandTypes.Length == 0 || AcceptableOperandTypes.Contains(operandAType)) &&
                    (AcceptableOperandTypes.Length == 0 || AcceptableOperandTypes.Contains(operandBType));
            }
        }
    }
}
