using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AssemblyGen.Serialization
{
    public interface ISerializationAssemblyBuilder
    {
        public void IncludeBinaryElementType(ISequentialBinaryElement elementType);
        public PersistedAssemblyBuilder BuildFromXml(XmlDocument xml);
    }
}
