using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen.Serialization
{
    public interface ISequentialBinaryType
    {
        public Type Type { get; }
        public void Serialize(object value, BinaryWriter writer);
        public object Deserialize(BinaryReader reader);
    }
}
