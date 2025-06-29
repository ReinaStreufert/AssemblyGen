﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public interface IMethodGeneratorContext<TSymbol, TAssignable> where TSymbol : class, ISymbol<TSymbol> where TAssignable : class, IAssignable<TSymbol>
    {
        public bool IsIterator { get; }
        public TSymbol? This { get; }
        public void Return();
        public void Return(TSymbol returnValue);
        public TSymbol Constant(object? value);
        public TAssignable GetArgument(Parameter parameter);
        public TAssignable DeclareLocal(Type type);
        public ILambdaBlock<TSymbol> BeginLambda(Type returnType, params Parameter[] parameters);
        public IIfBlock<TSymbol> BeginIfStatement(TSymbol condition);
        public ILoopBlock BeginLoop();
        public void Throw(TSymbol exception);
        public ITypeContext<TSymbol> Type(Type type);
        public TSymbol New(ConstructorInfo constructor, params TSymbol[] arguments);
        public TSymbol Delegate(MethodInfo method, Type delegateType, TSymbol? instance);
        public TSymbol Cast(Type type, TSymbol symbol);
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

    public interface IIfBlock<TSymbol> : IBlock where TSymbol : ISymbol
    {
        public void ElseIf(TSymbol symbol);
        public IBlock Else();
    }

    public interface ILoopBlock : IBlock
    {
        public void Break();
        public void Continue();
    }
}
