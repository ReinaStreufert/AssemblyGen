using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen.Serialization
{
    public interface ISerializationBuildContext
    {
        public Type BuildSequentialBinaryType(string name);
    }
}
