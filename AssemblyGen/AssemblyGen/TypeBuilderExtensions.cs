using System;
using System.Collections;
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

        public static MethodBuilder DefineIterator(this TypeBuilder typeBuilder, string name, MethodAttributes attributes, MethodGenerator generator, Type elementType, params Parameter[] parameters)
        {
            var enumerableInterface = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumerableTypeBuilder = typeBuilder.DefineNestedType(Identifier.Random(), TypeAttributes.NestedPrivate);
            enumerableTypeBuilder.AddInterfaceImplementation(enumerableInterface);
            enumerableTypeBuilder.AddInterfaceImplementation(typeof(IEnumerable));
            var thisField = attributes.HasFlag(MethodAttributes.Static) ? null : enumerableTypeBuilder.DefineField(Identifier.Random(), typeBuilder, FieldAttributes.Public);
            var enumerableParameterFields = parameters
                .Select(p => new KeyValuePair<Parameter, FieldBuilder>(p, enumerableTypeBuilder.DefineField(Identifier.Random(), p.Type, FieldAttributes.Public)))
                .ToImmutableDictionary();

            var enumeratorInterface = typeof(IEnumerator<>).MakeGenericType(elementType);
            var enumeratorTypeBuilder = typeBuilder.DefineNestedType(Identifier.Random(), TypeAttributes.NestedPrivate);
            enumeratorTypeBuilder.AddInterfaceImplementation(enumeratorInterface);
            enumeratorTypeBuilder.AddInterfaceImplementation(typeof(IEnumerator));
            var currentItemField = enumeratorTypeBuilder.DefineField(Identifier.Random(), elementType, FieldAttributes.Public);
            var iterationStateField = enumeratorTypeBuilder.DefineField(Identifier.Random(), typeof(int), FieldAttributes.Public);
            var enumerableField = enumeratorTypeBuilder.DefineField(Identifier.Random(), enumerableTypeBuilder, FieldAttributes.Public);

            var moveNextMeth = enumeratorTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly | MethodAttributes.Virtual);
            moveNextMeth.SetParameters();
            moveNextMeth.SetReturnType(typeof(bool));
            var generatorCtx = new IteratorGeneratorContext(enumeratorTypeBuilder, moveNextMeth.GetILGenerator(), elementType, enumerableParameterFields, currentItemField, iterationStateField, enumerableField, thisField);
            generator(generatorCtx);
            generatorCtx.Flush();
            MethodGenerator getCurrentGenerator = ctx =>
            {
                var inst = ctx.This!;
                var ifStatement = ctx.BeginIfStatement(inst
                    .Get(iterationStateField)
                    .Operation(BinaryOperator.EqualTo, ctx.Constant(0)));
                ctx.Throw(ctx.Type(typeof(InvalidOperationException)).New());
                ifStatement.End();
                ctx.Return(ctx.This!.Get(currentItemField));
            };
            var getCurrentMethod = enumeratorTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly | MethodAttributes.Virtual, getCurrentGenerator, elementType);
            var getCurrentNonGeneric = enumeratorTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly | MethodAttributes.Virtual, getCurrentGenerator, typeof(object));
            var resetMethod = enumeratorTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly | MethodAttributes.Virtual, ctx =>
            {
                ctx.This!.Set(iterationStateField, ctx.Constant(0));
                ctx.Return();
            }, typeof(void));
            var disposeMethod = enumeratorTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Assembly | MethodAttributes.Virtual, ctx =>
            {
                ctx.Return();
            }, typeof(void));
            enumeratorTypeBuilder.DefineMethodOverride(getCurrentMethod, enumeratorInterface.GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!);
            enumeratorTypeBuilder.DefineMethodOverride(getCurrentNonGeneric, typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!);
            enumeratorTypeBuilder.DefineMethodOverride(resetMethod, typeof(IEnumerator).GetMethod(nameof(IEnumerator.Reset))!);
            enumeratorTypeBuilder.DefineMethodOverride(disposeMethod, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!);
            enumeratorTypeBuilder.DefineMethodOverride(moveNextMeth, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!);
            var enumeratorConstructor = enumeratorTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var enumeratorType = enumeratorTypeBuilder.CreateType();

            MethodGenerator getEnumeratorGenerator = (IMethodGeneratorContext ctx) =>
            {
                var enumerator = ctx.DeclareLocal(enumeratorTypeBuilder);
                enumerator.Assign(ctx.New(enumeratorConstructor));
                enumerator.Set(enumerableField, ctx.This!);
                ctx.Return(enumerator);
            };
            var getEnumeratorMethod = enumerableTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual, getEnumeratorGenerator, enumeratorInterface);
            var getEnumeratorNonGeneric = enumerableTypeBuilder.DefineMethod(Identifier.Random(), MethodAttributes.Public | MethodAttributes.Virtual, getEnumeratorGenerator, typeof(IEnumerator));
            enumerableTypeBuilder.DefineMethodOverride(getEnumeratorMethod, enumerableInterface.GetMethod(nameof(IEnumerable.GetEnumerator))!);
            enumerableTypeBuilder.DefineMethodOverride(getEnumeratorNonGeneric, typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!);
            var enumerableConstructor = enumerableTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var enumerableType = enumerableTypeBuilder.CreateType();

            return typeBuilder.DefineMethod(name, attributes, ctx =>
            {
                var enumerable = ctx.DeclareLocal(enumerableTypeBuilder);
                enumerable.Assign(ctx.New(enumerableConstructor));
                foreach (var parameter in parameters)
                    enumerable.Set(enumerableParameterFields[parameter], ctx.GetArgument(parameter));
                ctx.Return(enumerable);
            }, enumerableInterface, parameters);
        }
    }
}
