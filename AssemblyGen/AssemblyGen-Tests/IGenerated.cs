using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen_Tests
{
    public interface IGenerated
    {
        public void WriteText(string text, ConditionSet set, int repititionCount);
    }

    public struct ConditionSet
    {
        public bool A;
        public bool B;
        public bool C;
    }
}
