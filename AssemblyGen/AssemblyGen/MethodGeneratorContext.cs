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
    public partial class MethodGeneratorContext : IMethodGeneratorContext
    {
        public virtual Symbol? This => _IsStatic ? null : new ArgumentSymbol(_Target, 0, _TypeBuilder, 0);
        public virtual bool IsIterator => false;

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

        protected TypeBuilder _TypeBuilder;
        protected ILGenerator _Il;
        protected Type _ReturnType;
        protected ImmutableDictionary<Parameter, int> _ParameterIndices;
        protected bool _IsStatic;
        protected int _BlockLevel = 0;
        protected int _ClosureLevel = 0;
        protected List<IEmittable> _EmitList = new List<IEmittable>();
        protected GeneratorTarget _Target;

        public virtual void Flush()
        {
            if (_BlockLevel > 0)
                throw new InvalidOperationException($"There are unclosed blocks");
            var il = _Il;
            foreach (var emittable in _EmitList)
                emittable.Emit(il);
        }

        public Symbol Constant(object? value) => new ConstantSymbol(_Target, value);

        public IIfBlock<Symbol> BeginIfStatement(Symbol condition)
        {
            var branchBegin = _Il.DefineLabel();
            _Target.Put(ILExpressionNode.BranchTrue(Symbol.Take(condition, typeof(bool)), branchBegin));
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

        public virtual AssignableSymbol GetArgument(Parameter parameter)
        {
            if (!_ParameterIndices.TryGetValue(parameter, out var argIndex))
                throw new ArgumentException($"{nameof(parameter)} was not declared in the method signature");
            return new ArgumentSymbol(_Target, argIndex, parameter.Type, _ClosureLevel);
        }

        public virtual AssignableSymbol DeclareLocal(Type type)
        {
            return new LocalSymbol(_Target, _Il.DeclareLocal(type), _ClosureLevel);
        }

        public virtual void Return()
        {
            if (_ReturnType != typeof(void))
                throw new InvalidOperationException($"Method must return a value of type '{_ReturnType.Name}'");
            _Target.Put(ILExpressionNode.Return());
        }

        public virtual void Return(Symbol returnValue)
        {
            if (_ReturnType == typeof(void))
                throw new InvalidOperationException($"Method of type 'void' does not return a value");
            if (!returnValue.Type.IsAssignableTo(_ReturnType))
                throw new ArgumentException($"$method of type '{_ReturnType.Name}' may not return a value of type '{returnValue.Type.Name}'");
            _Target.Put(ILExpressionNode.Return(Symbol.Take(returnValue, _ReturnType)));
        }

        public ILambdaBlock<Symbol> BeginLambda(Type returnType, params Parameter[] parameters)
        {
            var closureTypeBuilder = _TypeBuilder.DefineNestedType(Identifier.Random(), TypeAttributes.NestedPrivate | TypeAttributes.Class);
            var constructor = closureTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var closureLocal = _Il.DeclareLocal(closureTypeBuilder);
            var newClosureIl = new List<IILExpressionNode>
                {
                    ILExpressionNode.StoreLocal(
                        closureLocal.LocalIndex,
                        ILExpressionNode.NewObject(constructor))
                };
            var invocationMethod = closureTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly);
            invocationMethod.SetParameters(parameters.Select(p => p.Type).ToArray());
            invocationMethod.SetReturnType(returnType);
            var bodyIl = invocationMethod.GetILGenerator();
            var block = new LambdaBlock(this, _BlockLevel++, closureTypeBuilder, invocationMethod, bodyIl, closureLocal, newClosureIl);
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

        public void Throw(Symbol exception)
        {
            _Target.Put(ILExpressionNode.Throw(Symbol.Take(exception, typeof(Exception))));
        }

        public ITypeContext<Symbol> Type(Type type)
        {
            return new TypeContext(type, _Target);
        }

        protected abstract class Block : IBlock
        {
            public bool HasEnded => _HasEnded;

            public Block(MethodGeneratorContext ctx, int level)
            {
                Ctx = ctx;
                _Level = level;
            }

            protected MethodGeneratorContext Ctx;
            protected int _Level;
            protected bool _HasEnded = false;

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

        protected interface IEmittable
        {
            public void Emit(ILGenerator il);
        }
    }
}
