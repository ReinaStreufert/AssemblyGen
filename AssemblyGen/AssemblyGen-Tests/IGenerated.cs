using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen_Tests
{
    public interface IGenerated
    {
        public void WriteText(string text, ConditionSet set);
    }

    public struct ConditionSet
    {
        public bool A;
        public bool B;
        public bool C;
    }
}
