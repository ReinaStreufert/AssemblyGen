using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class BufferElement : ISequentialBinaryElement
    {
        public string ElementTypeName => "buffer";

        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType)
        {
            var fieldName = metadata.GetAttribute("name");
            var field = binaryType.DefineField(fieldName, typeof(byte[]), System.Reflection.FieldAttributes.Public);
            var length = int.Parse(metadata.GetAttribute("length"));
            return new BufferElementBuilder(field, length);
        }

        private class BufferElementBuilder : IBinaryElementBuilder
        {
            private FieldBuilder _Field;
            private int _Length;

            public BufferElementBuilder(FieldBuilder field, int length)
            {
                _Field = field;
                _Length = length;
            }

            public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value)
            {
                value.Set(_Field, binaryReader.Call(nameof(BinaryReader.ReadBytes), ctx.Constant(_Length)));
            }

            public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value)
            {
                binaryWriter.Call(nameof(BinaryWriter.Write), value.Get(_Field));
            }
        }
    }
}
