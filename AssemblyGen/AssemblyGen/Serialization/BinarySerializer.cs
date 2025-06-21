using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen.Serialization
{
    public static class BinarySerializer
    {
        private static Dictionary<Type, ISequentialBinaryType> _BinaryTypeDict = new Dictionary<Type, ISequentialBinaryType>();

        public static void IncludeBinaryType(ISequentialBinaryType binaryType)
        {
            _BinaryTypeDict.Add(binaryType.Type, binaryType);
        }

        public static void Serialize<T>(T value, BinaryWriter stream) where T : notnull
        {
            GetBinaryType(typeof(T)).Serialize(value, stream);
        }

        public static T Deserialize<T>(BinaryReader reader) where T : notnull
        {
            return (T)(GetBinaryType(typeof(T)).Deserialize(reader));
        }

        public static void Serialize<T>(T value, Stream stream) where T : notnull
        {
            using (var writer = new BinaryWriter(stream))
                Serialize(value, writer);
        }

        public static T Deserialize<T>(Stream stream) where T : notnull
        {
            using (var reader = new BinaryReader(stream))
                return Deserialize<T>(reader);
        }

        private static ISequentialBinaryType GetBinaryType(Type type)
        {
            if (!_BinaryTypeDict.TryGetValue(type, out var binaryType))
                throw new ArgumentException(nameof(type));
            return binaryType;
        }
    }
}
