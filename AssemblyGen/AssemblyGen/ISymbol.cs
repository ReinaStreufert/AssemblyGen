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

    public interface IAssignable : ISymbol
    {
        public void Assign(ISymbol value);
    }

    public interface IGeneratorTarget
    {
        public IStatement Put(IILExpressionNode node);
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
