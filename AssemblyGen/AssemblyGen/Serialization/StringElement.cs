using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class StringElement : ISequentialBinaryElement
    {
        public string ElementTypeName => "string";

        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType)
        {
            var fieldName = metadata.GetAttribute("name");
            var field = binaryType.DefineField(fieldName, typeof(string), System.Reflection.FieldAttributes.Public);
            return new StringElementBuilder(field);
        }

        private class StringElementBuilder : IBinaryElementBuilder
        {
            private FieldBuilder _Field;

            public StringElementBuilder(FieldBuilder field)
            {
                _Field = field;
            }

            public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value)
            {
                var length = ctx.DeclareLocal(typeof(int));
                length.Assign(binaryReader.Call(nameof(BinaryReader.Read7BitEncodedInt)));
                var textBuffer = ctx.DeclareLocal(typeof(byte[]));
                textBuffer.Assign(binaryReader.Call(nameof(BinaryReader.ReadBytes), length));
                value.Set(_Field, ctx.Type(typeof(Encoding)).Get(nameof(Encoding.UTF8)).Call(nameof(Encoding.GetString), textBuffer));
            }

            public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value)
            {
                var textBuffer = ctx.DeclareLocal(typeof(byte[]));
                textBuffer.Assign(ctx.Type(typeof(Encoding)).Get(nameof(Encoding.UTF8)).Call(nameof(Encoding.GetBytes), value.Get(_Field)));
                binaryWriter.Call(nameof(BinaryWriter.Write7BitEncodedInt), textBuffer.Get(nameof(Array.Length)));
                binaryWriter.Call(nameof(BinaryWriter.Write), textBuffer);
            }
        }
    }
}
