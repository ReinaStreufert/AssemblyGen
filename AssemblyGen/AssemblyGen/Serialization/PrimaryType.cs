using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen.Serialization
{
    public class PrimaryType
    {
        private static ImmutableDictionary<string, PrimaryType> _Dict;

        public static PrimaryType? TryFromName(string name) => _Dict.TryGetValue(name, out var primaryType) ? primaryType : null;

        static PrimaryType()
        {
            var primaryTypes = new PrimaryType[]
            {
                new("byte", typeof(byte), 1, nameof(BinaryReader.ReadByte)),
                new("sbyte", typeof(sbyte), 1, nameof(BinaryReader.ReadSByte)),
                new("short", typeof(short), 2, nameof(BinaryReader.ReadInt16)),
                new("ushort", typeof(ushort), 2, nameof(BinaryReader.ReadUInt16)),
                new("int", typeof(int), 4, nameof(BinaryReader.ReadInt32)),
                new("uint", typeof(uint), 4, nameof(BinaryReader.ReadUInt32)),
                new("long", typeof(long), 8, nameof(BinaryReader.ReadInt64)),
                new("ulong", typeof(ulong), 8, nameof(BinaryReader.ReadUInt64))
            };
            _Dict = primaryTypes.ToImmutableDictionary(t => t.Name);
        }

        private PrimaryType(string name, Type type, int size, string readerMethodName)
        {
            Name = name;
            Type = type;
            Size = size;
            BinaryReaderMethod = typeof(BinaryReader).GetMethod(readerMethodName)!;
            BinaryWriterMethod = typeof(BinaryWriter).GetMethod(nameof(BinaryWriter.Write), [type])!;
        }

        public string Name { get; }
        public Type Type { get; }
        public int Size { get; }
        public MethodInfo BinaryReaderMethod { get; }
        public MethodInfo BinaryWriterMethod { get; }
    }
}
