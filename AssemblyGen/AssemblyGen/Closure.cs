using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public partial class MethodGeneratorContext
    {
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
                }
                else
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
    }
}
