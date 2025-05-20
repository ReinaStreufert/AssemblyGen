using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml.Linq;

namespace AssemblyGen
{
    public abstract class Symbol : ISymbol
    {
        protected static IILExpressionNode Take(Symbol symbol)
        {
            return symbol.TakeAsExpressionNode();
        }

        public abstract Type Type { get; }

        public Symbol(IGeneratorTarget destination)
        {
            Destination = destination;
        }

        protected IGeneratorTarget Destination { get; }

        public Symbol CallMethod(string name, params Symbol[] args)
        {
            var argTypes = args
                .Select(a => a.Type)
                .ToArray();
            var method = Type.MatchMethod(name, argTypes);
            if (method == null)
                throw new ArgumentException($"No matching overload found '{Type.Name}.{name}({string.Join(", ", argTypes.Select(t => t.Name))})'");
            return MethodCallSymbol.Create(Destination, method, this, args);
        }

        public Symbol CallMethod(MethodInfo method, params Symbol[] args)
        {
            if (method.DeclaringType != Type)
                throw new ArgumentException(nameof(method));
            var argTypes = args
                .Select(a => a.Type)
                .ToArray();
            var matchedMethod = method.TryMatchMethod(method);
            if (method == null)
                throw new ArgumentException($"No matching overload found '{Type.Name}.{method.Name}({string.Join(", ", argTypes.Select(t => t.Name))})'");
            return MethodCallSymbol.Create(Destination, matchedMethod, this, args);
        }

        public Symbol GetFieldOrProperty(string name)
        {
            var field = Type.GetField(name);
            if (field != null)
                return FieldGetSymbol.Create(Destination, this, field);
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            return GetProperty(property);
        }

        private Symbol GetProperty(PropertyInfo property)
        {
            var getter = property.GetGetMethod() ??
                throw new ArgumentException($"No public get accessor found for property '{Type.Name}.{property.Name}'");
            return MethodCallSymbol.Create(Destination, getter, this);
        }

        public void SetFieldOrProperty(string name, Symbol value)
        {
            var field = Type.GetField(name);
            if (field != null)
            {
                if (!value.Type.IsAssignableTo(field.FieldType))
                    throw new ArgumentException($"type '{value.Type.Name}' is not assignable to field of type '{field.FieldType.Name}'");
                Destination.Put(ILExpressionNode.StoreField(Take(this), field, Take(value)));
                return;
            }
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            return SetProperty(property);
        }

        private void SetProperty(PropertyInfo property, Symbol value)
        {
            if (!value.Type.IsAssignableTo(property.PropertyType))
                throw new ArgumentException($"type '{value.Type.Name}' is not assignable to property of type '{property.PropertyType.Name}'");
            var setter = property.GetSetMethod() ??
                throw new ArgumentException($"No public set accessor found for property '{Type.Name}.{property.Name}'");
            Destination.Put(ILExpressionNode.MethodCall(Take(this), setter, false, Take(value)));
        }

        protected abstract IILExpressionNode TakeAsExpressionNode();
    }

    public abstract class IntermediateSymbol : Symbol
    {
        protected IntermediateSymbol(IGeneratorTarget destination) : base(destination)
        {
        }

        private bool _ValueTaken = false;

        protected override IILExpressionNode TakeAsExpressionNode()
        {
            if (_ValueTaken)
                throw new InvalidOperationException("The intermediate symbol has already been used in an expression");
            _ValueTaken = true;
            return ToExpressionNode();
        }

        protected abstract IILExpressionNode ToExpressionNode();
    }

    public abstract class AssignableSymbol : Symbol, IAssignable<Symbol>
    {
        protected AssignableSymbol(IGeneratorTarget destination) : base(destination)
        {
        }

        public abstract void Assign(Symbol value);
    }
}
