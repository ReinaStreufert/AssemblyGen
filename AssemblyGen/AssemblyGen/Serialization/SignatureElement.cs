using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public class SignatureElement : ISequentialBinaryElement
    {
        public string ElementTypeName => "signature";

        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType)
        {
            var hexString = metadata.GetAttribute("hex");
            return new SignatureElementBuilder(Convert.FromHexString(hexString));
        }

        private class SignatureElementBuilder : IBinaryElementBuilder
        {
            public SignatureElementBuilder(byte[] expectedValue)
            {
                _ExpectedValue = expectedValue;
            }

            private byte[] _ExpectedValue;

            public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value)
            {
                for (int i = 0; i < _ExpectedValue.Length; i++)
                {
                    var expectedByte = ctx.Constant(_ExpectedValue[i]);
                    var throwCondition = binaryReader.Call(nameof(BinaryReader.ReadByte))
                        .Operation(BinaryOperator.EqualTo, expectedByte)
                        .Operation(UnaryOperator.Not);
                    var ifStatement = ctx.BeginIfStatement(throwCondition);
                    ctx.Throw(ctx.Type(typeof(FormatException)).New());
                    ifStatement.End();
                }
            }

            public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value)
            {
                for (int i = 0; i < _ExpectedValue.Length; i++)
                    binaryWriter.Call(nameof(BinaryWriter.Write), ctx.Constant(_ExpectedValue[i]));
            }
        }
    }
}
