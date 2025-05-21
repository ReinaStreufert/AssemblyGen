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

    public interface IMemberable<TSymbol> : ISymbol where TSymbol : ISymbol
    {
        public TSymbol CallMethod(string name, params TSymbol[] args);
        public TSymbol CallMethod(MethodInfo method, params TSymbol[] args);
        public TSymbol GetFieldOrProperty(string name);
        public void SetFieldOrProperty(string name, TSymbol value);
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
}
