using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public interface IMethodGeneratorContext<TSymbol, TAssignable> where TSymbol : class, ISymbol where TAssignable : class, IAssignable<TSymbol>
    {
        public TSymbol? This { get; }
        public void Return();
        public void Return(TSymbol returnValue);
        public TAssignable GetArgument(Parameter parameter);
        public TAssignable DeclareLocal(Type type);
        public ILambdaBlock<TSymbol> Lambda(Type returnType, params Parameter[] parameters);
        public IBlock BeginIfStatement(TSymbol condition);
        public ILoopBlock BeginLoop();
    }

    public interface IMethodGeneratorContext : IMethodGeneratorContext<Symbol, AssignableSymbol>
    {

    }

    public delegate void MethodGenerator(IMethodGeneratorContext ctx);

    public interface IBlock
    {
        public bool HasEnded { get; }
        public void End();
    }

    public interface ILambdaBlock<TSymbol> : IBlock where TSymbol : ISymbol
    {
        public TSymbol ToDelegate(Type delegateType);
    }

    public interface IConditionalBlock : IBlock
    {
        public IConditionalBlock ElseIf(ISymbol symbol);
        public IBlock Else();
    }

    public interface ILoopBlock : IBlock
    {
        public void Break();
    }
}
