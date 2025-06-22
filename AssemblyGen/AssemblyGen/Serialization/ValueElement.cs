using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class ValueElement : ISequentialBinaryElement
    {
        public string ElementTypeName => "value";

        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType)
        {
            var typeName = metadata.GetAttribute("type");
            var type = PrimaryType.TryFromName(typeName) ?? throw new FormatException($"'{typeName}' is not a serializable type");
            var fieldName = metadata.GetAttribute("name");
            var field = binaryType.DefineField(fieldName, type.Type, System.Reflection.FieldAttributes.Public);
            return new ValueElementBuilder(type, field);
        }

        private class ValueElementBuilder : IBinaryElementBuilder
        {
            private PrimaryType _Type;
            private FieldBuilder _Field;

            public ValueElementBuilder(PrimaryType type, FieldBuilder field)
            {
                _Type = type;
                _Field = field;
            }

            public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value)
            {
                value.Set(_Field, binaryReader.Call(_Type.BinaryReaderMethod));
            }

            public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value)
            {
                binaryWriter.Call(_Type.BinaryWriterMethod, value.Get(_Field));
            }
        }
    }
}
