using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public interface ISequentialBinaryElement
    {
        public string ElementTypeName { get; }
        public IBinaryElementBuilder GetBuilder(XmlElement metadata, TypeBuilder binaryType, ISerializationBuildContext buildContext);
        
    }
}
