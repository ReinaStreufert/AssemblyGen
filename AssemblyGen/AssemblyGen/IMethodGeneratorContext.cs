using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public interface IMethodGeneratorContext
    {
        public void Return();
        public void Return(ISymbol returnValue);
        public ISymbol GetArgument(Parameter parameter);
        public ISymbol Local(Type type);
        public ISymbol Lambda(MethodGenerator generator, IEnumerable<Parameter> parameters);
        public ISymbol Lambda(MethodGenerator generator, params Parameter[] parameters);
        public IBlock BeginIfStatement(ISymbol condition);
        public ILoopBlock BeginLoop();
    }

    public delegate void MethodGenerator(IMethodGeneratorContext ctx);

    public interface IBlock
    {
        public void End();
    }

    public interface IConditionalBlock
    {
        public IConditionalBlock ElseIf(ISymbol symbol);
        public IBlock Else();
    }

    public interface ILoopBlock
    {
        public void Break();
    }
}
