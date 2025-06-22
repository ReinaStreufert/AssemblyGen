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
        public PersistedAssemblyBuilder BuildFromXml(XmlDocument xml)
        {
            var docElement = xml.DocumentElement!;
            if (docElement.Name != "serializerAssembly")
                throw new ArgumentException(nameof(xml));
            var assemblyName = docElement.GetAttribute("name");
            var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), Assembly.GetAssembly(typeof(object))!);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(Identifier.Random());
            
        }

        public void IncludeBinaryElementType(ISequentialBinaryElement elementType)
        {
            throw new NotImplementedException();
        }

        private ConstructorBuilder ImplementBinaryTypeSerializer(ModuleBuilder moduleBuilder, IEnumerable<IBinaryElementBuilder> elementBuilders, TypeBuilder binaryType, ConstructorBuilder binaryTypeConstructor)
        {
            var binaryTypeSerializerBuilder = moduleBuilder.DefineType(Identifier.Random());
            binaryTypeSerializerBuilder.AddInterfaceImplementation(typeof(IBinaryTypeSerializer));
            MethodGenerator typeGetGenerator = (IMethodGeneratorContext ctx) =>
            {
                ctx.Return(ctx.Type(binaryType).GetReflectedTypeObject());
            };
            var typeGetMethodBuilder = binaryTypeSerializerBuilder.DefineMethod("Type_get", MethodAttributes.Assembly | MethodAttributes.Virtual, typeGetGenerator, typeof(Type));
            binaryTypeSerializerBuilder.DefineMethodOverride(typeGetMethodBuilder, typeof(IBinaryTypeSerializer).GetProperty(nameof(IBinaryTypeSerializer.Type))!.GetGetMethod()!);
            var valueParameter = typeof(object).AsParameter();
            var binaryWriterParameter = typeof(BinaryWriter).AsParameter();
            MethodGenerator serializeGenerator = (IMethodGeneratorContext ctx) =>
            {
                var value = ctx.GetArgument(valueParameter);
                var binaryWriter = ctx.GetArgument(binaryWriterParameter);
                foreach (var elementBuilder in elementBuilders)
                    elementBuilder.GenerateSerializer(ctx, binaryWriter, value);
            };
            var serializerMethodBuilder = binaryTypeSerializerBuilder.DefineMethod("Serialize_impl", MethodAttributes.Assembly | MethodAttributes.Virtual, serializeGenerator, typeof(void), valueParameter, binaryWriterParameter);
            binaryTypeSerializerBuilder.DefineMethodOverride(serializerMethodBuilder, typeof(IBinaryTypeSerializer).GetMethod(nameof(IBinaryTypeSerializer.Serialize))!);
            var binaryReaderParameter = typeof(BinaryReader).AsParameter();
            /*MethodGenerator deserializeGenerator = (IMethodGeneratorContext ctx) =>
            {
                var value = ctx.DeclareLocal(binaryType);
                foreach (var elementBuilder in elementBuilders)
                    elementBuilder.
            };*/
        }
    }
}
