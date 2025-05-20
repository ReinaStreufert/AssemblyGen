using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public static class TypeBuilderExtensions
    {
        public static void DefineMethod(this TypeBuilder typeBuilder, string name, MethodAttributes attributes, MethodGenerator generator, Type returnType, params Parameter[] parameters)
        {
            var methBuilder = typeBuilder.DefineMethod(name, attributes);
            methBuilder.SetParameters(parameters.Select(p => p.Type).ToArray());
        }

        private class MethodGeneratorContext : IMethodGeneratorContext
        {
            public MethodGeneratorContext(ILGenerator il, Type returnType, Parameter[] parameters)
            {
                _Il = il;
                _ReturnType = returnType;
                _ParameterIndices = parameters
                    .Select((p, i) => new KeyValuePair<Parameter, int>(p, i))
                    .ToImmutableDictionary();
                _Target = new GeneratorTarget(_EmitList);
            }

            private TypeBuilder? _TypeBuilder;
            private ILGenerator _Il;
            private Type _ReturnType;
            private ImmutableDictionary<Parameter, int> _ParameterIndices;
            private Stack<IBlock> _OpenBlocks = new Stack<IBlock>();
            private List<IEmittable> _EmitList = new List<IEmittable>();
            private GeneratorTarget _Target;

            public void Flush()
            {
                var il = _Il;
                foreach (var emittable in _EmitList)
                    emittable.Emit(il);
            }

            public IBlock BeginIfStatement(Symbol condition)
            {
                
                throw new NotImplementedException();
            }

            public ILoopBlock BeginLoop()
            {
                throw new NotImplementedException();
            }

            public AssignableSymbol GetArgument(Parameter parameter)
            {
                if (!_ParameterIndices.TryGetValue(parameter, out var argIndex))
                    throw new ArgumentException($"{nameof(parameter)} was not declared in the method signature");
                return new ArgumentSymbol(_Target, argIndex, parameter.Type);
            }

            public AssignableSymbol DeclareLocal(Type type)
            {
                return new LocalSymbol(_Target, _Il.DeclareLocal(type));
            }

            public void Return()
            {
                throw new NotImplementedException();
            }

            public void Return(Symbol returnValue)
            {
                throw new NotImplementedException();
            }

            public Symbol Lambda(MethodGenerator generator, IEnumerable<Parameter> parameters)
            {
                throw new NotImplementedException();
            }

            public Symbol Lambda(MethodGenerator generator, params Parameter[] parameters)
            {
                throw new NotImplementedException();
            }

            private class GeneratorTarget : IGeneratorTarget
            {
                public GeneratorTarget(List<IEmittable> dst)
                {
                    _EmitList = dst;
                }

                private List<IEmittable> _EmitList;

                public IStatement Put(IILExpressionNode node)
                {
                    var statement = new Statement(node);
                    _EmitList.Add(statement);
                    return statement;
                }

                private class Statement : IStatement, IEmittable
                {
                    public Statement(IILExpressionNode node)
                    {
                        _Node = node;
                    }

                    private bool _Withdrawn = false;
                    private IILExpressionNode _Node;

                    public void Withdraw()
                    {
                        _Withdrawn = true;
                    }

                    public void Emit(ILGenerator il)
                    {
                        if (!_Withdrawn)
                            _Node.WriteInstructions(il);
                    }
                }
            }

            private interface IEmittable
            {
                public void Emit(ILGenerator il);
            }
        }
    }
}
