using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace AssemblyGen
{
    public class IteratorGeneratorContext : MethodGeneratorContext
    {
        public override Symbol? This => _ThisField == null ? null : _EnumerableInst.Get(_ThisField);

        public IteratorGeneratorContext(TypeBuilder typeBuilder, ILGenerator il, Type returnType, ImmutableDictionary<Parameter, FieldBuilder> parameters, FieldBuilder currentItemField, FieldBuilder iterationStateField, FieldBuilder enumerableField, FieldBuilder? thisField) : base(typeBuilder, il, returnType, new Parameter[] { }, thisField == null)
        {
            // typeBuilder represents the IEnumerator implementation
            // il represents MoveNext implementation
            // returnType represents element type, aka return type of Current property
            // thisField represents field on IEnumerator implementation which stores the instance on which the IEnumerable-instantiator method was called
            var enumerableInst = base.DeclareLocal(enumerableField.FieldType);
            enumerableInst.Assign(_EnumeratorInst.Get(enumerableField));
            _EnumerableInst = enumerableInst;
            _ThisField = thisField;
            _IterationStateField = iterationStateField;
            _CurrentItemField = currentItemField;
            _EnumerableField = enumerableField;
            _LoadedParameterLocals = LoadParameterFields(parameters).ToImmutableDictionary();
            _Target.Put(new RestoreStateExpressionNode(this));
        }

        private Symbol _EnumeratorInst => base.This!;
        private Symbol _EnumerableInst;

        private FieldBuilder? _ThisField;
        private FieldBuilder _CurrentItemField;
        private FieldBuilder _IterationStateField;
        private FieldBuilder _EnumerableField;
        private ImmutableDictionary<Parameter, AssignableSymbol> _LoadedParameterLocals;
        private List<KeyValuePair<int, FieldBuilder>> _LocalStateFields = new List<KeyValuePair<int, FieldBuilder>>();
        private List<Label> _YieldStateLabels = new List<Label>();

        private IEnumerable<KeyValuePair<Parameter, AssignableSymbol>> LoadParameterFields(IEnumerable<KeyValuePair<Parameter, FieldBuilder>> parameters)
        {
            foreach (var pair in parameters)
            {
                var local = base.DeclareLocal(pair.Key.Type);
                local.Assign(_EnumerableInst.Get(pair.Value));
                yield return new KeyValuePair<Parameter, AssignableSymbol>(pair.Key, local);
            }
        }

        public override AssignableSymbol GetArgument(Parameter parameter)
        {
            if (_ClosureLevel > 0)
                return base.GetArgument(parameter);
            if (!_LoadedParameterLocals.TryGetValue(parameter, out var argumentLocal))
                throw new ArgumentException($"{nameof(parameter)} was not declared in the method signature");
            return argumentLocal;
        }

        public override AssignableSymbol DeclareLocal(Type type)
        {
            if (_ClosureLevel > 0)
                return base.DeclareLocal(type);
            var localBuilder = _Il.DeclareLocal(type);
            var stateField = _TypeBuilder.DefineField(Identifier.Random(), type, System.Reflection.FieldAttributes.Public);
            _LocalStateFields.Add(new KeyValuePair<int, FieldBuilder>(localBuilder.LocalIndex, stateField));
            return new LocalSymbol(_Target, localBuilder);
        }

        public override void Return(Symbol returnValue) // acts as yield return
        {
            if (_ClosureLevel > 0)
            {
                base.Return(returnValue);
                return;
            }
            var yieldResumeLabel = _Il.DefineLabel();
            _YieldStateLabels.Add(yieldResumeLabel);
            _Target.Put(new SaveStateExpressionNode(this, _YieldStateLabels.Count));
            _EnumeratorInst.Set(_CurrentItemField, returnValue);
            base.Return(Constant(true));
            _Target.Put(ILExpressionNode.Label(yieldResumeLabel));
        }

        public override void Return() // acts as yield break
        {
            if (_ClosureLevel > 0)
            {
                base.Return();
                return;
            }
            _EnumeratorInst.Set(_IterationStateField, Constant(-1));
            base.Return(Constant(false));
        }

        private class SaveStateExpressionNode : IILExpressionNode
        {
            // iteration state field key
            // =0: iterator begin
            // >0: yield state; one-based index of _YieldStateLabels representing entry point to resume iteration from
            // <0: iterator sequence has ended

            public SaveStateExpressionNode(IteratorGeneratorContext genCtx, int iterationStateIndex)
            {
                _GenCtx = genCtx;
            }

            private IteratorGeneratorContext _GenCtx;
            private int _IterationStateIndex;

            public void WriteInstructions(ILGenerator il)
            {
                foreach (var pair in _GenCtx._LocalStateFields)
                {
                    ILExpressionNode.StoreField(ILExpressionNode.LoadThis, pair.Value, ILExpressionNode.LoadLocal(pair.Key))
                        .WriteInstructions(il);
                }
                ILExpressionNode.StoreField(ILExpressionNode.LoadThis, _GenCtx._IterationStateField, ILExpressionNode.Constant(_IterationStateIndex))
                    .WriteInstructions(il);
            }
        }

        private class RestoreStateExpressionNode : IILExpressionNode
        {
            public RestoreStateExpressionNode(IteratorGeneratorContext genCtx)
            {
                _GenCtx = genCtx;
            }

            private IteratorGeneratorContext _GenCtx;

            public void WriteInstructions(ILGenerator il)
            {
                var iterationStateIndex = il.DeclareLocal(typeof(int));
                ILExpressionNode.StoreLocal(iterationStateIndex.LocalIndex, ILExpressionNode.LoadField(ILExpressionNode.LoadThis, _GenCtx._IterationStateField))
                    .WriteInstructions(il);
                var iteratorBeginLabel = il.DefineLabel();
                ILExpressionNode.BranchTrue(
                    ILExpressionNode.Equal(ILExpressionNode.LoadLocal(iterationStateIndex.LocalIndex), ILExpressionNode.Constant(0)), 
                    iteratorBeginLabel)
                    .WriteInstructions(il);
                var iteratorEndLabel = il.DefineLabel();
                ILExpressionNode.BranchTrue(
                    ILExpressionNode.LessThan(ILExpressionNode.LoadLocal(iterationStateIndex.LocalIndex), ILExpressionNode.Constant(0), false),
                    iteratorEndLabel)
                    .WriteInstructions(il);
                foreach (var pair in _GenCtx._LocalStateFields)
                {
                    ILExpressionNode.StoreLocal(pair.Key, ILExpressionNode.LoadField(ILExpressionNode.LoadThis, pair.Value))
                        .WriteInstructions(il);
                }
                for (int i = 0; i < _GenCtx._YieldStateLabels.Count; i++)
                {
                    var label = _GenCtx._YieldStateLabels[i];
                    ILExpressionNode.BranchTrue(
                        ILExpressionNode.Equal(ILExpressionNode.LoadLocal(iterationStateIndex.LocalIndex), ILExpressionNode.Constant(i + 1)),
                        label)
                        .WriteInstructions(il);
                }
                ILExpressionNode.Branch(iteratorBeginLabel)
                    .WriteInstructions(il);
                il.MarkLabel(iteratorEndLabel);
                ILExpressionNode.Return(ILExpressionNode.Constant(false))
                    .WriteInstructions(il);
                il.MarkLabel(iteratorBeginLabel);
            }
        }
    }
}
