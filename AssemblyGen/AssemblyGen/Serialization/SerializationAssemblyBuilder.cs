using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class SerializationAssemblyBuilder : ISerializationAssemblyBuilder
    {
        private Dictionary<string, ISequentialBinaryElement> _ElementDict = new Dictionary<string, ISequentialBinaryElement>();

        public PersistedAssemblyBuilder BuildFromXml(XmlDocument xml)
        {
            var docElement = xml.DocumentElement!;
            if (docElement.Name != "serializerAssembly")
                throw new FormatException();
            var assemblyName = docElement.GetAttribute("name");
            var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), Assembly.GetAssembly(typeof(object))!);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(Identifier.Random());
            List<ConstructorInfo> binaryTypeSerializers = new List<ConstructorInfo>();
            foreach (var childElement in docElement.ChildNodes.OfType<XmlElement>())
            {
                if (childElement.Name != "sequentialBinaryType")
                    throw new FormatException();
                var binaryTypeName = childElement.GetAttribute("name");
                var binaryType = moduleBuilder.DefineType(binaryTypeName, TypeAttributes.Class | TypeAttributes.Public);
                var elementBuilders = GetElementBuilders(childElement, binaryType)
                    .ToArray();
                var binaryTypeConstructor = binaryType.DefineDefaultConstructor(MethodAttributes.Assembly);
                binaryTypeSerializers.Add(ImplementBinaryTypeSerializer(moduleBuilder, elementBuilders, binaryType.CreateType(), binaryTypeConstructor));
            }
            MethodGenerator moduleInitializerGenerator = ctx =>
            {
                var binarySerializerStaticType = ctx.Type(typeof(BinarySerializer));
                foreach (var serializerConstructor in binaryTypeSerializers)
                    binarySerializerStaticType.Call(nameof(BinarySerializer.IncludeBinaryType), ctx.New(serializerConstructor));
                ctx.Return();
            };
            // global static methods named .cctor are initializers which run before any other code
            var globalMethod = moduleBuilder.DefineGlobalMethod(".cctor", MethodAttributes.Assembly | MethodAttributes.Static, null, null);
            var globalMethodGenCtx = new MethodGeneratorContext(null, globalMethod.GetILGenerator(), typeof(void), Array.Empty<Parameter>(), true);
            moduleInitializerGenerator(globalMethodGenCtx);
            globalMethodGenCtx.Flush();
            moduleBuilder.CreateGlobalFunctions();
            return assemblyBuilder;
        }

        public void IncludeBinaryElementType(ISequentialBinaryElement elementType)
        {
            _ElementDict.Add(elementType.ElementTypeName, elementType);
        }

        public void IncludeDefaultElementTypes()
        {
            IncludeBinaryElementType(new ValueElement());
            IncludeBinaryElementType(new BufferElement());
            IncludeBinaryElementType(new VarBufferElement());
            IncludeBinaryElementType(new SignatureElement());
        }

        private IEnumerable<IBinaryElementBuilder> GetElementBuilders(XmlElement binaryTypeXmlNode, TypeBuilder binaryType)
        {
            foreach (var childXmlElement in binaryTypeXmlNode.ChildNodes.OfType<XmlElement>())
            {
                if (!_ElementDict.TryGetValue(childXmlElement.Name, out var binaryElement))
                    throw new FormatException();
                yield return binaryElement.GetBuilder(childXmlElement, binaryType);
            }
        }

        private ConstructorBuilder ImplementBinaryTypeSerializer(ModuleBuilder moduleBuilder, IEnumerable<IBinaryElementBuilder> elementBuilders, Type binaryType, ConstructorBuilder binaryTypeConstructor)
        {
            var binaryTypeSerializerBuilder = moduleBuilder.DefineType(Identifier.Random(), TypeAttributes.Class);
            binaryTypeSerializerBuilder.AddInterfaceImplementation(typeof(IBinaryTypeSerializer));
            MethodGenerator typeGetGenerator = ctx =>
            {
                ctx.Return(ctx.Type(binaryType).GetReflectedTypeObject());
            };
            var typeGetMethodBuilder = binaryTypeSerializerBuilder.DefineMethod("Type_get", MethodAttributes.Assembly | MethodAttributes.Virtual, typeGetGenerator, typeof(Type));
            binaryTypeSerializerBuilder.DefineMethodOverride(typeGetMethodBuilder, typeof(IBinaryTypeSerializer).GetProperty(nameof(IBinaryTypeSerializer.Type))!.GetGetMethod()!);
            var valueParameter = typeof(object).AsParameter();
            var binaryWriterParameter = typeof(BinaryWriter).AsParameter();
            MethodGenerator serializeGenerator = ctx =>
            {
                var uncastedValue = ctx.GetArgument(valueParameter);
                var value = ctx.DeclareLocal(binaryType);
                value.Assign(ctx.Cast(binaryType, uncastedValue));
                var binaryWriter = ctx.GetArgument(binaryWriterParameter);
                foreach (var elementBuilder in elementBuilders)
                    elementBuilder.GenerateSerializer(ctx, binaryWriter, value);
                ctx.Return();
            };
            var serializerMethodBuilder = binaryTypeSerializerBuilder.DefineMethod("Serialize_impl", MethodAttributes.Assembly | MethodAttributes.Virtual, serializeGenerator, typeof(void), valueParameter, binaryWriterParameter);
            binaryTypeSerializerBuilder.DefineMethodOverride(serializerMethodBuilder, typeof(IBinaryTypeSerializer).GetMethod(nameof(IBinaryTypeSerializer.Serialize))!);
            var binaryReaderParameter = typeof(BinaryReader).AsParameter();
            MethodGenerator deserializeGenerator = ctx =>
            {
                var binaryReader = ctx.GetArgument(binaryReaderParameter);
                var value = ctx.DeclareLocal(binaryType);
                value.Assign(ctx.New(binaryTypeConstructor));
                foreach (var elementBuilder in elementBuilders)
                    elementBuilder.GenerateDeserializer(ctx, binaryReader, value);
                ctx.Return(value);
            };
            var deserializerMethodBuilder = binaryTypeSerializerBuilder.DefineMethod("Deserialize_impl", MethodAttributes.Assembly | MethodAttributes.Virtual, deserializeGenerator, typeof(object), binaryReaderParameter);
            binaryTypeSerializerBuilder.DefineMethodOverride(deserializerMethodBuilder, typeof(IBinaryTypeSerializer).GetMethod(nameof(IBinaryTypeSerializer.Deserialize))!);
            var constructor = binaryTypeSerializerBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            binaryTypeSerializerBuilder.CreateType();
            return constructor;
        }
    }
}
