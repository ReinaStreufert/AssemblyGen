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
        public static MethodBuilder DefineMethod(this TypeBuilder typeBuilder, string name, MethodAttributes attributes, MethodGenerator generator, Type returnType, params Parameter[] parameters)
        {
            var methBuilder = typeBuilder.DefineMethod(name, attributes);
            methBuilder.SetParameters(parameters.Select(p => p.Type).ToArray());
            methBuilder.SetReturnType(returnType);
            var generatorCtx = new MethodGeneratorContext(typeBuilder, methBuilder.GetILGenerator(), returnType, parameters, attributes.HasFlag(MethodAttributes.Static));
            generator(generatorCtx);
            generatorCtx.Flush();
            return methBuilder;
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
                if (_BlockLevel > 0)
                    throw new InvalidOperationException($"There are unclosed blocks");
                var il = _Il;
                foreach (var emittable in _EmitList)
                    emittable.Emit(il);
            }

            public Symbol Constant(object? value) => new ConstantSymbol(_Target, value);

            public IMemberable<Symbol> StaticType(Type type) => new StaticMemberAccessor(type, _Target);

            public IIfBlock<Symbol> BeginIfStatement(Symbol condition)
            {
                if (condition.Type != typeof(bool))
                    throw new ArgumentException($"{nameof(condition)} must be of type boolean");
                var branchBegin = _Il.DefineLabel();
                _Target.Put(ILExpressionNode.BranchTrue(Symbol.Take(condition), branchBegin));
                var branchBody = new List<IEmittable>();
                var conditionalBranch = new Branch(branchBegin, branchBody);
                var ifBlock = new IfBlock(this, _BlockLevel++, new List<Branch>() { conditionalBranch });
                _EmitList = branchBody;
                return ifBlock;
            }

            public ILoopBlock BeginLoop()
            {
                var repeatLabel = _Il.DefineLabel();
                _Target.Put(ILExpressionNode.Label(repeatLabel));
                return new LoopBlock(this, _BlockLevel++, repeatLabel);
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
                if (_ReturnType != typeof(void))
                    throw new InvalidOperationException($"Method must return a value of type '{_ReturnType.Name}'");
                _Target.Put(ILExpressionNode.Return());
            }

            public void Return(Symbol returnValue)
            {
                if (_ReturnType == typeof(void))
                    throw new InvalidOperationException($"Method of type 'void' does not return a value");
                if (!returnValue.Type.IsAssignableTo(_ReturnType))
                    throw new ArgumentException($"$method of type '{_ReturnType.Name}' may not return a value of type '{returnValue.Type.Name}'");
                _Target.Put(ILExpressionNode.Return(Symbol.Take(returnValue)));
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
                var invocationMethod = closureTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
                invocationMethod.SetParameters(parameters.Select(p => p.Type).ToArray());
                invocationMethod.SetReturnType(returnType);
                var bodyIl = invocationMethod.GetILGenerator();
                var block = new LambdaBlock(this, _BlockLevel++, invocationMethod, bodyIl, closureLocal, newClosureIl);
                var closure = new Closure(_Target.CurrentClosure, _ClosureLevel++, closureTypeBuilder, closureLocal, newClosureIl);
                _Il = bodyIl;
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

                public void Put(IEnumerable<IEmittable> emittables)
                {
                    _Ctx._EmitList.AddRange(emittables);
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

            private class LoopBlock : Block, ILoopBlock
            {
                public LoopBlock(MethodGeneratorContext ctx, int level, Label repeatLabel) : base(ctx, level)
                {
                    _RepeatLabel = repeatLabel;
                    _EscapeLabel = ctx._Il.DefineLabel();
                }

                private Label _RepeatLabel;
                private Label _EscapeLabel;

                public void Break()
                {
                    if (HasEnded)
                        throw new InvalidOperationException("The loop has already ended");
                    Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
                }


                public void Continue()
                {
                    if (HasEnded)
                        throw new InvalidOperationException($"The loop has already ended");
                    Ctx._Target.Put(ILExpressionNode.Branch(_RepeatLabel));
                }

                public override void End()
                {
                    base.End();
                    Ctx._Target.Put(ILExpressionNode.Branch(_RepeatLabel));
                    Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
                }
            }

            private class IfBlock : Block, IIfBlock<Symbol>
            {
                public IfBlock(MethodGeneratorContext ctx, int level, List<Branch> branches) : base(ctx, level)
                {
                    _OuterEmitList = ctx._EmitList;
                    _EscapeLabel = ctx._Il.DefineLabel();
                    _Branches = branches;
                }

                private List<IEmittable> _OuterEmitList;
                private List<Branch> _Branches;
                private bool _Continued = false;
                private Label _EscapeLabel;

                public void ElseIf(Symbol condition)
                {
                    EnsureLevel();
                    if (condition.Type != typeof(bool))
                        throw new ArgumentException($"{nameof(condition)} must be of type boolean");
                    var branchBeginLabel = Ctx._Il.DefineLabel();
                    var branchBody = new List<IEmittable>();
                    Ctx._EmitList = _OuterEmitList;
                    Ctx._Target.Put(ILExpressionNode.BranchTrue(Symbol.Take(condition), branchBeginLabel));
                    Ctx._EmitList = branchBody;
                    _Branches.Add(new Branch(branchBeginLabel, branchBody));
                }

                public IBlock Else()
                {
                    if (_Continued)
                        throw new InvalidOperationException("If statement may only have one fallback else statement");
                    _Continued = true;
                    Ctx._EmitList = _OuterEmitList;
                    return new ElseBlock(Ctx, EnsureLevel(), _Branches, _EscapeLabel);
                }

                public override void End()
                {
                    base.End();
                    if (_Continued)
                        throw new InvalidOperationException("This if statement is continued to an open else block. end the statement on the else block");
                    Ctx._EmitList = _OuterEmitList;
                    Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
                    foreach (var branch in _Branches)
                        branch.Write(Ctx._Target, _EscapeLabel);
                    Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
                }
            }

            private class ElseBlock : Block
            {
                public ElseBlock(MethodGeneratorContext ctx, int level, List<Branch> branches, Label escapeLabel) : base(ctx, level)
                {
                    _Branches = branches;
                    _EscapeLabel = escapeLabel;
                }

                private List<Branch> _Branches;
                private Label _EscapeLabel;

                public override void End()
                {
                    base.End();
                    Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
                    foreach (var branch in _Branches)
                        branch.Write(Ctx._Target, _EscapeLabel);
                    Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
                }
            }

            private struct Branch
            {
                public Label _BeginLabel;
                public List<IEmittable> _EmitList;

                public Branch(Label beginLabel, List<IEmittable> emitList)
                {
                    _BeginLabel = beginLabel;
                    _EmitList = emitList;
                }

                public void Write(GeneratorTarget target, Label escapeLabel)
                {
                    target.Put(ILExpressionNode.Label(_BeginLabel));
                    target.Put(_EmitList);
                    target.Put(ILExpressionNode.Branch(escapeLabel));
                }
            }

            private class LambdaBlock : Block, ILambdaBlock<Symbol>
            {
                public LambdaBlock(MethodGeneratorContext ctx, int level, MethodBuilder invocationMethod, ILGenerator bodyIl, LocalBuilder closureInstLocal, List<IILExpressionNode> newClosureIl) : base(ctx, level)
                {
                    _InvocationMethod = invocationMethod;
                    _BodyIl = bodyIl;
                    _ClosureInstLocal = closureInstLocal;
                    _OuterEmitList = ctx._EmitList;
                    _OuterParameterIndices = ctx._ParameterIndices;
                    _OuterReturnType = ctx._ReturnType;
                    _OuterClosure = ctx._Target.CurrentClosure;
                    _OuterIl = ctx._Il;
                    _NewClosureIl = newClosureIl;
                }

                private LocalBuilder _ClosureInstLocal;
                private List<IEmittable> _OuterEmitList;
                private List<IILExpressionNode> _NewClosureIl;
                private ImmutableDictionary<Parameter, int> _OuterParameterIndices;
                private Type _OuterReturnType;
                private IClosure? _OuterClosure;
                private ILGenerator _OuterIl;
                private MethodBuilder _InvocationMethod;
                private ILGenerator _BodyIl;

                public override void End()
                {
                    base.End();
                    var lambdaBodyEmitList = Ctx._EmitList;
                    Ctx._Il = _OuterIl;
                    Ctx._EmitList = _OuterEmitList;
                    Ctx._ParameterIndices = _OuterParameterIndices;
                    Ctx._ReturnType = _OuterReturnType;
                    Ctx._ClosureLevel = _OuterClosure == null ? 0 : _OuterClosure.Level + 1;
                    var target = Ctx._Target;
                    target.CurrentClosure = _OuterClosure;
                    target.Put(ILExpressionNode.Sequential(_NewClosureIl));
                    foreach (var emittable in lambdaBodyEmitList)
                        emittable.Emit(_BodyIl);
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
                    Ctx._BlockLevel = EnsureLevel();
                    _HasEnded = true;
                }

                protected int EnsureLevel()
                {
                    if (Ctx._BlockLevel < _Level + 1 || _HasEnded)
                        throw new InvalidOperationException("The block has already ended");
                    if (Ctx._BlockLevel > _Level + 1)
                        throw new InvalidOperationException("All blocks opened after this block must end before this block may end");
                    return _Level;
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
