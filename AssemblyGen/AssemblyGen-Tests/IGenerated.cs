using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen_Tests
{
    public interface IGenerated
    {
        public Action<string> WriteTextFactory(ConditionSet set, int repititionCount);
    }

    public class ConditionSet
    {
        public bool A { get; set; }
        public bool B { get; set; }
        public bool C { get; set; }
    }
}
