using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public interface IBinaryElementBuilder
    {
        public void GenerateSerializer(IMethodGeneratorContext ctx, AssignableSymbol binaryWriter, AssignableSymbol value);
        public void GenerateDeserializer(IMethodGeneratorContext ctx, AssignableSymbol binaryReader, AssignableSymbol value);
    }
}
