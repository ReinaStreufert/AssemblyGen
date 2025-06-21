using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public static class MethodGeneratorExtensions
    {
        private static AssemblyBuilder? _DynamicAsm;
        private static ModuleBuilder? _DynamicModule;

        public static ILoopBlock BeginForeachLoop(this IMethodGeneratorContext context, Symbol enumerable, out AssignableSymbol currentItem)
        {
            if (!enumerable.Type.IsAssignableTo(typeof(IEnumerable)))
                throw new ArgumentException($"{nameof(enumerable)} does not implement IEnumerable interface");
            var genericEnumerableType = enumerable.Type.GetInterfaces()
                .Prepend(enumerable.Type)
                .Where(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .FirstOrDefault();
            var elementType = genericEnumerableType?.GetGenericArguments()[0] ?? typeof(object);
            currentItem = context.DeclareLocal(elementType);
            var enumeratorCall = enumerable.Call(nameof(IEnumerable.GetEnumerator));
            var enumerator = context.DeclareLocal(enumeratorCall.Type);
            enumerator.Assign(enumeratorCall);
            var loop = context.BeginLoop();
            var escapeCondition = enumerator
                .Call(nameof(IEnumerator.MoveNext))
                .Operation(UnaryOperator.Not);
            var escapeCheck = context.BeginIfStatement(escapeCondition);
            loop.Break();
            escapeCheck.End();
            currentItem.Assign(enumerator.Get(nameof(IEnumerator.Current)));
            return loop;
        }

        public static TDelegate Compile<TDelegate>(this MethodGenerator methodGenerator, Type returnType, params Parameter[] parameters) where TDelegate : Delegate
        {
            if (_DynamicAsm == null)
            {
                _DynamicAsm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Identifier.Random()), AssemblyBuilderAccess.Run);
                _DynamicModule = _DynamicAsm.DefineDynamicModule(Identifier.Random());
            }
            var asmBuilder = _DynamicAsm;
            var moduleBuilder = _DynamicModule!;
            var typeBuilder = moduleBuilder.DefineType(Identifier.Random(), TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IRuntimeInvocationProvider));
            var meth = typeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Static, methodGenerator, returnType, parameters);
            var getDelegateMethod = typeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual, ctx =>
            {
                ctx.Return(ctx.Delegate(meth, typeof(TDelegate), null));
            }, typeof(TDelegate));
            typeBuilder.DefineMethodOverride(getDelegateMethod, typeof(IRuntimeInvocationProvider).GetMethod(nameof(IRuntimeInvocationProvider.GetDelegate))!);
            var generatedType = typeBuilder.CreateType();
            var invocationProvider = (IRuntimeInvocationProvider)Activator.CreateInstance(generatedType)!;
            return (TDelegate)invocationProvider.GetDelegate();
        }

        /*public static TDelegate CompileIterator<TDelegate>(this MethodGenerator methodGenerator, Type elementType, params Parameter[] parameters) where TDelegate : Delegate
        {
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Identifier.Random()), AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule(Identifier.Random());
            var typeBuilder = moduleBuilder.DefineType(Identifier.Random(), TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(typeof(IRuntimeInvocationProvider));
            var meth = typeBuilder.DefineIterator(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Static, methodGenerator, elementType, parameters);
            var getDelegateMethod = typeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual, ctx =>
            {
                ctx.Return(ctx.Delegate(meth, typeof(TDelegate), null));
            }, typeof(Delegate));
            typeBuilder.DefineMethodOverride(getDelegateMethod, typeof(IRuntimeInvocationProvider).GetMethod(nameof(IRuntimeInvocationProvider.GetDelegate))!);
            var generatedType = typeBuilder.CreateType();
            var invocationProvider = (IRuntimeInvocationProvider)Activator.CreateInstance(generatedType)!;
            return (TDelegate)invocationProvider.GetDelegate();
        }*/
    }
}
