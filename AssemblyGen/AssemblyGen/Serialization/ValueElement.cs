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

        public IBinaryElementBuilder GetBuilder(XmlElement metadata)
        {
            throw new NotImplementedException();
        }

        private class ValueElementBuilder : IBinaryElementBuilder
        {

        }
    }
}
