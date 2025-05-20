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
            public Symbol? This => _IsStatic ? null : new ArgumentSymbol(_Target, 0, _TypeBuilder, 0);

            public MethodGeneratorContext(TypeBuilder typeBuilder, ILGenerator il, Type returnType, Parameter[] parameters, bool isStatic)
            {
                _Il = il;
                _ReturnType = returnType;
                _IsStatic = isStatic;
                _ParameterIndices = parameters
                    .Select((p, i) => new KeyValuePair<Parameter, int>(p, _IsStatic ? i : i + 1))
                    .ToImmutableDictionary();
                _Target = new GeneratorTarget(this);
                _TypeBuilder = typeBuilder;
            }

            private TypeBuilder _TypeBuilder;
            private ILGenerator _Il;
            private Type _ReturnType;
            private ImmutableDictionary<Parameter, int> _ParameterIndices;
            private bool _IsStatic;
            private int _BlockLevel = 0;
            private int _ClosureLevel = 0;
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
                return new ArgumentSymbol(_Target, argIndex, parameter.Type, _ClosureLevel);
            }

            public AssignableSymbol DeclareLocal(Type type)
            {
                return new LocalSymbol(_Target, _Il.DeclareLocal(type), _ClosureLevel);
            }

            public void Return()
            {
                throw new NotImplementedException();
            }

            public void Return(Symbol returnValue)
            {
                throw new NotImplementedException();
            }

            public ILambdaBlock<Symbol> Lambda(Type returnType, params Parameter[] parameters)
            {
                var closureTypeBuilder = _TypeBuilder.DefineNestedType(Identifier.Random(), TypeAttributes.NestedPrivate);
                var constructor = closureTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
                var closureLocal = _Il.DeclareLocal(closureTypeBuilder);
                var newClosureIl = new List<IILExpressionNode>
                {
                    ILExpressionNode.StoreLocal(
                        closureLocal.LocalIndex,
                        ILExpressionNode.NewObject(constructor))
                };
                var invocationMethod = closureTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual);
                invocationMethod.SetParameters(parameters.Select(p => p.Type).ToArray());
                invocationMethod.SetReturnType(returnType);
                var block = new LambdaBlock(this, _BlockLevel++, invocationMethod, closureLocal, newClosureIl);
                var closure = new Closure(_Target.CurrentClosure, _ClosureLevel++, closureTypeBuilder, closureLocal, newClosureIl);
                _EmitList = new List<IEmittable>();
                _ReturnType = returnType;
                _ParameterIndices = parameters
                    .Select((p, i) => new KeyValuePair<Parameter, int>(p, i + 1))
                    .ToImmutableDictionary();
                _Target.CurrentClosure = closure;
                return block;
            }

            private class GeneratorTarget : IGeneratorTarget
            {
                public IClosure? CurrentClosure { get; set; }

                public GeneratorTarget(MethodGeneratorContext ctx)
                {
                    _Ctx = ctx;
                }

                private MethodGeneratorContext _Ctx;

                public IStatement Put(IILExpressionNode node)
                {
                    var statement = new Statement(node);
                    _Ctx._EmitList.Add(statement);
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

            private class LambdaBlock : Block, ILambdaBlock<Symbol>
            {
                public LambdaBlock(MethodGeneratorContext ctx, int level, MethodBuilder invocationMethod, LocalBuilder closureInstLocal, List<IILExpressionNode> newClosureIl) : base(ctx, level)
                {
                    _InvocationMethod = invocationMethod;
                    _ClosureInstLocal = closureInstLocal;
                    _OuterEmitList = ctx._EmitList;
                    _OuterParameterIndices = ctx._ParameterIndices;
                    _OuterReturnType = ctx._ReturnType;
                    _OuterClosure = ctx._Target.CurrentClosure;
                    _NewClosureIl = newClosureIl;
                }

                private LocalBuilder _ClosureInstLocal;
                private List<IEmittable> _OuterEmitList;
                private List<IILExpressionNode> _NewClosureIl;
                private ImmutableDictionary<Parameter, int> _OuterParameterIndices;
                private Type _OuterReturnType;
                private IClosure? _OuterClosure;
                private MethodBuilder _InvocationMethod;

                public override void End()
                {
                    base.End();
                    var lambdaBodyEmitList = Ctx._EmitList;
                    Ctx._EmitList = _OuterEmitList;
                    Ctx._ParameterIndices = _OuterParameterIndices;
                    Ctx._ReturnType = _OuterReturnType;
                    Ctx._ClosureLevel = _OuterClosure == null ? 0 : _OuterClosure.Level + 1;
                    var target = Ctx._Target;
                    target.CurrentClosure = _OuterClosure;
                    target.Put(ILExpressionNode.Sequential(_NewClosureIl));
                    var bodyIl = _InvocationMethod.GetILGenerator();
                    foreach (var emittable in lambdaBodyEmitList)
                        emittable.Emit(bodyIl);
                }

                public Symbol ToDelegate(Type delegateType)
                {
                    if (!HasEnded)
                        throw new InvalidOperationException($"The lambda is still open");
                    if (!delegateType.IsAssignableTo(typeof(Delegate)))
                        throw new ArgumentException($"{nameof(delegateType)} is not a delegate type");
                    var delegatePtr = ILExpressionNode.LoadVirtualFunction(
                        ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                        _InvocationMethod);
                    return new LambdaDelegateSymbol(Ctx._Target, delegatePtr, delegateType);
                }
            }

            private abstract class Block : IBlock
            {
                public bool HasEnded => _HasEnded;

                public Block(MethodGeneratorContext ctx, int level)
                {
                    Ctx = ctx;
                    _Level = level;
                }

                protected MethodGeneratorContext Ctx;
                private int _Level;
                private bool _HasEnded = false;

                public virtual void End()
                {
                    if (Ctx._BlockLevel < _Level || _HasEnded)
                        throw new InvalidOperationException("The block has already ended");
                    if (Ctx._BlockLevel > _Level)
                        throw new InvalidOperationException("All blocks opened after this block must end before this block may end");
                    _HasEnded = true;
                    Ctx._BlockLevel = _Level - 1;
                }
            }

            private class Closure : IClosure
            {
                public int Level => _Level;

                public Closure(IClosure? parentClosure, int level, TypeBuilder closureTypeBuilder, LocalBuilder closureInstLocal, List<IILExpressionNode> newClosureIl)
                {
                    _ParentClosure = parentClosure;
                    _Level = level;
                    _ClosureTypeBuilder = closureTypeBuilder;
                    _ClosureInstLocal = closureInstLocal;
                    _NewClosureIl = newClosureIl;
                }

                private IClosure? _ParentClosure;
                private int _Level;
                private TypeBuilder _ClosureTypeBuilder;
                private LocalBuilder _ClosureInstLocal;
                private List<IILExpressionNode> _NewClosureIl = new List<IILExpressionNode>();
                private Dictionary<int, FieldInfo> _CapturedArguments = new Dictionary<int, FieldInfo>();
                private Dictionary<int, FieldInfo> _CapturedLocals = new Dictionary<int, FieldInfo>();
                private Dictionary<FieldInfo, FieldInfo> _CapturedParentClosureFields = new Dictionary<FieldInfo, FieldInfo>();

                public FieldInfo CaptureArgument(int argumentIndex, int level, Type valueType)
                {
                    if (level < _Level)
                    {
                        var containingClosureField = _ParentClosure!.CaptureArgument(argumentIndex, level, valueType);
                        if (_CapturedParentClosureFields.TryGetValue(containingClosureField, out var capturedContainingClosureField))
                            return capturedContainingClosureField;
                        var captureValueField = _ClosureTypeBuilder.DefineField(Identifier.Random(), containingClosureField.FieldType, FieldAttributes.Public);
                        _NewClosureIl.Add(ILExpressionNode.StoreField(
                            ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                            captureValueField,
                            ILExpressionNode.LoadField(ILExpressionNode.LoadThis, containingClosureField)));
                        _CapturedParentClosureFields.Add(containingClosureField, captureValueField);
                        return captureValueField;
                    } else
                    {
                        if (_CapturedArguments.TryGetValue(argumentIndex, out var capturedArgumentField))
                            return capturedArgumentField;
                        var captureValueField = _ClosureTypeBuilder.DefineField(Identifier.Random(), valueType, FieldAttributes.Public);
                        _NewClosureIl.Add(ILExpressionNode.StoreField(
                            ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                            captureValueField,
                            ILExpressionNode.LoadArgument(argumentIndex)));
                        _CapturedArguments.Add(argumentIndex, captureValueField);
                        return captureValueField;
                    }
                }

                public FieldInfo CaptureLocal(int localIndex, int level, Type valueType)
                {
                    if (level < _Level)
                    {
                        var containingClosureField = _ParentClosure!.CaptureLocal(localIndex, level, valueType);
                        if (_CapturedParentClosureFields.TryGetValue(containingClosureField, out var capturedContainingClosureField))
                            return capturedContainingClosureField;
                        var captureValueField = _ClosureTypeBuilder.DefineField(Identifier.Random(), containingClosureField.FieldType, FieldAttributes.Public);
                        _NewClosureIl.Add(ILExpressionNode.StoreField(
                            ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                            captureValueField,
                            ILExpressionNode.LoadField(ILExpressionNode.LoadThis, containingClosureField)));
                        _CapturedParentClosureFields.Add(containingClosureField, captureValueField);
                        return captureValueField;
                    }
                    else
                    {
                        if (_CapturedLocals.TryGetValue(localIndex, out var capturedLocalField))
                            return capturedLocalField;
                        var captureValueField = _ClosureTypeBuilder.DefineField(Identifier.Random(), valueType, FieldAttributes.Public);
                        _NewClosureIl.Add(ILExpressionNode.StoreField(
                            ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                            captureValueField,
                            ILExpressionNode.LoadLocal(localIndex)));
                        _CapturedLocals.Add(localIndex, captureValueField);
                        return captureValueField;
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
