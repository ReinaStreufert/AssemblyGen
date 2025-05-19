using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public static class ILExpressionNode
    {
        public static ILNode Constant(object? compileTimeCst)
        {
            Action<ILGenerator> generator = compileTimeCst switch
            {
                null => il => il.Emit(OpCodes.Ldc_I4_0),
                bool cst => il => il.Emit(OpCodes.Ldc_I4_S, cst ? 1 : 0),
                string cst => il => il.Emit(OpCodes.Ldstr, cst),
                byte cst => il => il.Emit(OpCodes.Ldc_I4_S, cst),
                sbyte cst => il => il.Emit(OpCodes.Ldc_I4_S, (byte)cst),
                ushort cst => il => il.Emit(OpCodes.Ldc_I4, (uint)cst),
                short cst => il => il.Emit(OpCodes.Ldc_I4, (uint)cst),
                uint cst => il => il.Emit(OpCodes.Ldc_I4, cst),
                int cst => il => il.Emit(OpCodes.Ldc_I4, (uint)cst),
                ulong cst => il => il.Emit(OpCodes.Ldc_I8, cst),
                long cst => il => il.Emit(OpCodes.Ldc_I8, (ulong)cst),
                decimal cst => il => il.Emit(OpCodes.Ldc_R4, (float)cst),
                float cst => il => il.Emit(OpCodes.Ldc_R4, cst),
                double cst => il => il.Emit(OpCodes.Ldc_R8, cst),
                _ => throw new ArgumentException($"value of {nameof(compileTimeCst)} cannot be a compile-time constant")
            };
            return new ILNode(generator);
        }

        public static ILNode LoadField(IILExpressionNode? instance, FieldInfo field)
        {
            return new ILNode(il =>
            {
                if (instance != null)
                    instance.WriteInstructions(il);
                il.Emit(OpCodes.Ldfld, field);
            });
        }

        public static ILNode StoreField(IILExpressionNode? instance, FieldInfo field, IILExpressionNode value)
        {
            return new ILNode(il =>
            {
                if (instance != null)
                    instance.WriteInstructions(il);
                value.WriteInstructions(il);
                il.Emit(OpCodes.Stfld, field);
            });
        }

        public static ILMethodCallNode MethodCall(IILExpressionNode? instance, MethodInfo method, bool preserveReturnValue = true, params IILExpressionNode[] arguments)
            => new ILMethodCallNode(instance, method, arguments, preserveReturnValue);
    }

    public class ILNode : IILExpressionNode
    {
        public ILNode(Action<ILGenerator> generatorCallback)
        {
            _Callback = generatorCallback;
        }

        private Action<ILGenerator> _Callback;

        public void WriteInstructions(ILGenerator il)
        {
            _Callback(il);
        }
    }

    public class ILMethodCallNode : IILExpressionNode
    {
        public IILExpressionNode? Instance { get; }
        public MethodInfo Method { get; }
        public IILExpressionNode[] Arguments { get; }
        public bool PreserveReturnValue { get; set; }

        public ILMethodCallNode(IILExpressionNode? instance, MethodInfo method, IILExpressionNode[] arguments, bool preserveReturnVal = true)
        {
            if ((instance == null) != method.IsStatic)
                throw new ArgumentException(nameof(instance));
            Instance = instance;
            Method = method;
            Arguments = FillDefaultedParameters( method.GetParameters(), arguments);
            PreserveReturnValue = preserveReturnVal;
        }

        public void WriteInstructions(ILGenerator il)
        {
            if (Instance != null)
                Instance.WriteInstructions(il);
            foreach (var arg in Arguments)
                arg.WriteInstructions(il);
            il.Emit(OpCodes.Call, Method);
            if (!PreserveReturnValue && Method.ReturnType != typeof(void))
                il.Emit(OpCodes.Pop);
        }

        private static IILExpressionNode[] FillDefaultedParameters(ParameterInfo[] methodParameters, IILExpressionNode[] arguments)
        {
            if (arguments.Length == methodParameters.Length)
                return arguments;
            if (methodParameters.Length < arguments.Length)
                throw new ArgumentException(nameof(arguments));
            var result = new IILExpressionNode[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i < arguments.Length)
                {
                    result[i] = arguments[i];
                    continue;
                }
                var parameter = methodParameters[i];
                if (!parameter.HasDefaultValue)
                    throw new ArgumentException(nameof(arguments));

                result[i] = ILExpressionNode.Constant(parameter.RawDefaultValue);
            }
            return result;
        }
    }
}
