using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public static class MethodGeneratorExtensions
    {
        public static ILoopBlock BeginForeachLoop(this IMethodGeneratorContext context, Symbol enumerable, out AssignableSymbol currentItem)
        {
            if (!enumerable.Type.IsAssignableTo(typeof(IEnumerable)))
                throw new ArgumentException($"{nameof(enumerable)} does not implement IEnumerable interface");
            var genericEnumerableType = enumerable.Type.GetInterfaces()
                .Where(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .FirstOrDefault();
            var elementType = genericEnumerableType?.GetGenericArguments()[0] ?? typeof(object);
            currentItem = context.DeclareLocal(elementType);
            var enumeratorCall = enumerable.Call(nameof(IEnumerable.GetEnumerator));
            var enumerator = context.DeclareLocal(enumeratorCall.Type);
            enumerator.Assign(enumeratorCall);
            var loop = context.BeginLoop();
            var escapeCondition = enumerator
                .Call(nameof(IEnumerator.MoveNext))
                .Operation(UnaryOperator.Not);
            var escapeCheck = context.BeginIfStatement(escapeCondition);
            loop.Break();
            escapeCheck.End();
            currentItem.Assign(enumerator.Get(nameof(IEnumerator.Current)));
            return loop;
        }
    }
}
