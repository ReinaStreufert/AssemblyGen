using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public partial class MethodGeneratorContext
    {
        private class LambdaBlock : Block, ILambdaBlock<Symbol>
        {
            public LambdaBlock(MethodGeneratorContext ctx, int level, TypeBuilder closureTypeBuilder, MethodBuilder invocationMethod, ILGenerator bodyIl, LocalBuilder closureInstLocal, List<IILExpressionNode> newClosureIl) : base(ctx, level)
            {
                _InvocationMethod = invocationMethod;
                _ClosureTypeBuilder = closureTypeBuilder;
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
            private TypeBuilder _ClosureTypeBuilder;
            private MethodBuilder _InvocationMethod;
            private ILGenerator _BodyIl;

            public override void End()
            {
                base.End();
                var lambdaBodyEmitList = Ctx._EmitList;
                foreach (var emittable in lambdaBodyEmitList)
                    emittable.Emit(_BodyIl);
                Ctx._Il = _OuterIl;
                Ctx._EmitList = _OuterEmitList;
                Ctx._ParameterIndices = _OuterParameterIndices;
                Ctx._ReturnType = _OuterReturnType;
                Ctx._ClosureLevel = _OuterClosure == null ? 0 : _OuterClosure.Level + 1;
                var target = Ctx._Target;
                target.CurrentClosure = _OuterClosure;
                target.Put(ILExpressionNode.Sequential(_NewClosureIl));

                _ClosureTypeBuilder.CreateType();
            }

            public Symbol ToDelegate(Type delegateType)
            {
                if (!HasEnded)
                    throw new InvalidOperationException($"The lambda is still open");
                if (!delegateType.IsAssignableTo(typeof(Delegate)))
                    throw new ArgumentException($"{nameof(delegateType)} is not a delegate type");
                return new LambdaDelegateSymbol(
                    Ctx._Target,
                    ILExpressionNode.LoadLocal(_ClosureInstLocal.LocalIndex),
                    ILExpressionNode.LoadFunction(_InvocationMethod),
                    delegateType);
            }
        }
    }
}
