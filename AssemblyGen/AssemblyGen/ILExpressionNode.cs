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
        public static ILNode Branch(Label destination)
        {
            return new ILNode(il => il.Emit(OpCodes.Br, destination));
        }

        public static ILNode BranchTrue(IILExpressionNode condition, Label destination)
        {
            return new ILNode(il =>
            {
                condition.WriteInstructions(il);
                il.Emit(OpCodes.Brtrue, destination);
            });
        }

        public static ILNode Label(Label label)
        {
            return new ILNode(il => il.MarkLabel(label));
        }

        public static ILNode Constant(object? compileTimeCst)
        {
            Action<ILGenerator> generator = compileTimeCst switch
            {
                null => il => il.Emit(OpCodes.Ldnull),
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

        public static ILNode Sequential(IEnumerable<IILExpressionNode> nodes)
        {
            return new ILNode(il =>
            {
                foreach (var node in nodes)
                    node.WriteInstructions(il);
            });
        }

        public static ILNode NewObject(ConstructorInfo constructor, params IILExpressionNode[] arguments)
        {
            return new ILNode(il =>
            {
                foreach (var arg in arguments)
                    arg.WriteInstructions(il);
                il.Emit(OpCodes.Newobj, constructor);
            });
        }

        public static ILNode Return(IILExpressionNode? returnValue = null)
        {
            return new ILNode(il =>
            {
                if (returnValue != null)
                    returnValue.WriteInstructions(il);
                il.Emit(OpCodes.Ret);
            });
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

        public static ILNode LoadThis => LoadArgument(0);

        public static ILNode LoadArgument(int argumentIndex)
        {
            return new ILNode(il => il.Emit(OpCodes.Ldarg, argumentIndex));
        }

        public static ILNode StoreArgument(int parameterIndex, IILExpressionNode value)
        {
            return new ILNode(il =>
            {
                value.WriteInstructions(il);
                il.Emit(OpCodes.Starg, parameterIndex);
            });
        }

        public static ILNode LoadLocal(int localIndex)
        {
            return new ILNode(il => il.Emit(OpCodes.Ldloc, localIndex));
        }

        public static ILNode StoreLocal(int localIndex, IILExpressionNode value)
        {
            return new ILNode(il =>
            {
                value.WriteInstructions(il);
                il.Emit(OpCodes.Stloc, localIndex);
            });
        }

        public static ILMethodCallNode Call(IILExpressionNode? instance, MethodInfo method, bool preserveReturnValue = true, params IILExpressionNode[] arguments)
            => new ILMethodCallNode(instance, method, arguments, preserveReturnValue);

        public static ILNode LoadVirtualFunction(IILExpressionNode instance, MethodInfo method)
        {
            return new ILNode(il =>
            {
                instance.WriteInstructions(il);
                il.Emit(OpCodes.Ldvirtftn, method);
            });
        }
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
