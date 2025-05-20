using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public static class Identifier
    {
        private const int Length = 8;
        private const int MinChar = 'a';
        private const int MaxChar = 'z';
        private static readonly Random _Rand = new Random();

        public static string Random()
        {
            var charBuf = new char[Length];
            for (int i = 0; i < Length; i++)
                charBuf[i] = (char)_Rand.Next(MinChar, MaxChar + 1);
            return new string(charBuf);
        }
    }
}
