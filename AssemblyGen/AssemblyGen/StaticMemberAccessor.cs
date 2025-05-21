using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class StaticMemberAccessor : IMemberable<Symbol>
    {
        public Type Type => _Type;

        public StaticMemberAccessor(Type type, IGeneratorTarget destination)
        {
            _Type = type;
            _Destination = destination;
        }

        private Type _Type;
        private IGeneratorTarget _Destination;

        public Symbol CallMethod(string name, params Symbol[] args)
        {
            var argTypes = args
                .Select(a => a.Type)
                .ToArray();
            var method = _Type.MatchMethod(name, argTypes);
            if (method == null)
                throw new ArgumentException($"No matching overload found '{Type.Name}.{name}({string.Join(", ", argTypes.Select(t => t.Name))})'");
            if (!method.IsStatic)
                throw new ArgumentException($"Cannot call instance method '{Type.Name}.{name}' without an object reference");
            return MethodCallSymbol.Create(_Destination, method, null, args);
        }

        public Symbol CallMethod(MethodInfo method, params Symbol[] args)
        {
            if (!method.IsStatic)
                throw new ArgumentException($"Cannot call instance method without an object reference");
            var argTypes = args
                .Select(a => a.Type)
                .ToArray();
            var matchedMethod = method.TryMatchMethod(argTypes);
            if (matchedMethod == null)
                throw new ArgumentException($"Argument types do not match {nameof(method)}");
            return MethodCallSymbol.Create(_Destination, matchedMethod, null, args);
        }

        public Symbol GetFieldOrProperty(string name)
        {
            var field = Type.GetField(name);
            if (field != null)
            {
                if (!field.IsStatic)
                    throw new ArgumentException($"Cannot get the value of an instance field without an object reference");
                return FieldGetSymbol.Create(_Destination, null, field);
            }
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            var getter = property.GetGetMethod() ??
                throw new ArgumentException($"No public get accessor found for property '{Type.Name}.{name}'");
            if (!getter.IsStatic)
                throw new ArgumentException($"Cannot get instance property without an object reference");
            return MethodCallSymbol.Create(_Destination, getter, null);
        }

        public void SetFieldOrProperty(string name, Symbol value)
        {
            var field = Type.GetField(name);
            if (field != null)
            {
                if (!field.IsStatic)
                    throw new ArgumentException($"Cannot set the value of instance field without an object reference");
                if (!value.Type.IsAssignableTo(field.FieldType))
                    throw new ArgumentException($"type '{value.Type.Name}' is not assignable to field of type '{field.FieldType.Name}'");
                _Destination.Put(ILExpressionNode.StoreField(null, field, Symbol.Take(value)));
                return;
            }
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            var setter = property.GetSetMethod() ??
                throw new ArgumentException($"No public set accessor found for property '{Type.Name}.{property.Name}'");
            if (!setter.IsStatic)
                throw new ArgumentException($"Cannot set instance property witout and object reference");
            if (!value.Type.IsAssignableTo(property.PropertyType))
                throw new ArgumentException($"type '{value.Type.Name}' is not assignable to property of type '{property.PropertyType.Name}'");
            _Destination.Put(ILExpressionNode.Call(null, setter, false, Symbol.Take(value)));
        }
    }
}
