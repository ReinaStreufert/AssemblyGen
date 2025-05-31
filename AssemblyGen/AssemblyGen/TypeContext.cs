using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public class TypeContext : ITypeContext<Symbol>
    {
        public Type Type => _Type;

        public TypeContext(Type type, IGeneratorTarget destination)
        {
            _Type = type;
            _Destination = destination;
        }

        private Type _Type;
        private IGeneratorTarget _Destination;

        public Symbol Call(string name, params Symbol[] args)
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

        public Symbol Call(MethodInfo method, params Symbol[] args)
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

        public Symbol Get(string name)
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

        public Symbol Get(FieldInfo field)
        {
            if (!field.IsStatic)
                throw new ArgumentException($"Cannot get the value of an instance field without an object reference");
            return FieldGetSymbol.Create(_Destination, null, field);
        }

        public void Set(string name, Symbol value)
        {
            var field = Type.GetField(name);
            if (field != null)
            {
                if (!field.IsStatic)
                    throw new ArgumentException($"Cannot set the value of instance field without an object reference");
                _Destination.Put(ILExpressionNode.StoreField(null, field, Symbol.Take(value, field.FieldType)));
                return;
            }
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            var setter = property.GetSetMethod() ??
                throw new ArgumentException($"No public set accessor found for property '{Type.Name}.{property.Name}'");
            if (!setter.IsStatic)
                throw new ArgumentException($"Cannot set instance property witout and object reference");
            _Destination.Put(ILExpressionNode.Call(null, setter, false, Symbol.Take(value, property.PropertyType)));
        }

        public void Set(FieldInfo field, Symbol value)
        {
            if (!field.IsStatic)
                throw new ArgumentException($"Cannot set the value of instance field without an object reference");
            _Destination.Put(ILExpressionNode.StoreField(null, field, Symbol.Take(value, field.FieldType)));
        }

        public Symbol New(params Symbol[] args)
        {
            var constructor = _Type.MatchConstructor(args.Select(a => a.Type).ToArray()) ??
                throw new ArgumentException($"{nameof(args)} do not match any constructors");
            return new ConstructorCallSymbol(_Destination, constructor, args);
        }

        public Symbol GetReflectedTypeObject()
        {
            return new TypeOfSymbol(_Destination, _Type);
        }
    }
}
