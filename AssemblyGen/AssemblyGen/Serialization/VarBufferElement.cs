using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class VarBufferElement : ISequentialBinaryElement
    {
        public string ElementTypeName => "varBuffer";

        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType)
        {
            var fieldName = metadata.GetAttribute("name");
            var field = binaryType.DefineField(fieldName, typeof(byte[]), System.Reflection.FieldAttributes.Public);
            return new VarBufferElementBuilder(field);
        }

        private class VarBufferElementBuilder : IBinaryElementBuilder
        {
            private FieldBuilder _Field;
            
            public VarBufferElementBuilder(FieldBuilder field)
            {
                _Field = field;
            }

            public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value)
            {
                var lengthLocal = ctx.DeclareLocal(typeof(int));
                lengthLocal.Assign(binaryReader.Call(nameof(BinaryReader.ReadInt32)));
                value.Set(_Field, binaryReader.Call(nameof(BinaryReader.ReadBytes), lengthLocal));
            }

            public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value)
            {
                var arrayLocal = ctx.DeclareLocal(typeof(byte[]));
                arrayLocal.Assign(value.Get(_Field));
                binaryWriter.Call(nameof(BinaryWriter.Write), arrayLocal.Get(nameof(Array.Length)));
                binaryWriter.Call(nameof(BinaryWriter.Write), arrayLocal);
            }
        }
    }
}
