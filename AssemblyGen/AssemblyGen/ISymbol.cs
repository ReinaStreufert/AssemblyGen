using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public interface ISymbol
    {
        public Type Type { get; }
    }

    public interface ISymbol<TSelf> : ISymbol
    {
        public TSelf Operation(UnaryOperator op);
        public TSelf Operation(BinaryOperator op, TSelf operand);
    }

    public interface IMemberable<TSymbol> : ISymbol where TSymbol : ISymbol<TSymbol>
    {
        public TSymbol Call(string name, params TSymbol[] args);
        public TSymbol Call(MethodInfo method, params TSymbol[] args);
        public TSymbol Get(string name);
        public TSymbol Get(FieldInfo field);
        public void Set(string name, TSymbol value);
        public void Set(FieldInfo field, TSymbol value);
    }

    public interface ITypeContext<TSymbol> : IMemberable<TSymbol> where TSymbol : ISymbol<TSymbol>
    {
        public TSymbol New(params TSymbol[] args);
        public TSymbol NewArray(TSymbol length);
        public TSymbol GetReflectedTypeObject();
    }

    public interface IAssignable<TSymbol> : ISymbol where TSymbol : ISymbol
    {
        public void Assign(TSymbol value);
    }

    public interface IGeneratorTarget
    {
        public IClosure? CurrentClosure { get; }
        public IStatement Put(IILExpressionNode node);
    }

    public interface IClosure
    {
        public int Level { get; }
        public FieldInfo CaptureLocal(int localIndex, int closureLevel, Type valueType);
        public FieldInfo CaptureArgument(int argumentIndex, int closureLevel, Type valueType);
    }

    public interface IILExpressionNode
    {
        public void WriteInstructions(ILGenerator il);
    }

    public interface IStatement
    {
        public void Withdraw();
    }

    public enum BinaryOperator
    {
        LessThan,
        GreaterThan,
        EqualTo,
        And,
        Or,
        Plus,
        Minus,
        Times,
        Divide,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor
    }

    public enum UnaryOperator
    {
        Not,
        BitwiseNot
    }
}
