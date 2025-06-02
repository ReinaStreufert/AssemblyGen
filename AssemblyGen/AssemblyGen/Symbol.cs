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
    public abstract class Symbol : ISymbol<Symbol>, IMemberable<Symbol>
    {
        public static IILExpressionNode Take(Symbol symbol, Type toType)
        {
            if (!symbol.Type.IsAssignableTo(toType))
                throw new ArgumentException($"value of type '{symbol.Type.Name}' cannot be used in expression expecting type '{toType}'");
            return ILExpressionNode.ConvertImplicit(symbol.TakeAsExpressionNode(), symbol.Type, toType);
        }

        public abstract Type Type { get; }

        public Symbol(IGeneratorTarget destination)
        {
            Destination = destination;
        }

        protected IGeneratorTarget Destination { get; }

        public Symbol Call(string name, params Symbol[] args)
        {
            var argTypes = args
                .Select(a => a.Type)
                .ToArray();
            var method = Type.MatchMethod(name, argTypes);
            if (method == null)
                throw new ArgumentException($"No matching overload found '{Type.Name}.{name}({string.Join(", ", argTypes.Select(t => t.Name))})'");
            return MethodCallSymbol.Create(Destination, method, method.IsStatic ? null: this, args);
        }

        public Symbol Call(MethodInfo method, params Symbol[] args)
        {
            if (!Type.IsAssignableTo(method.DeclaringType))
                throw new ArgumentException(nameof(method));
            return MethodCallSymbol.Create(Destination, method, method.IsStatic ? null : this, args);
        }

        public Symbol Get(string name)
        {
            var field = Type.GetField(name);
            if (field != null)
                return FieldGetSymbol.Create(Destination, field.IsStatic ? null : this, field);
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            var getter = property.GetGetMethod() ??
                throw new ArgumentException($"No public get accessor found for property '{Type.Name}.{name}'");
            return MethodCallSymbol.Create(Destination, getter, getter.IsStatic ? null : this);
        }

        public Symbol Get(FieldInfo field)
        {
            if (!Type.IsAssignableTo(field.DeclaringType))
                throw new ArgumentException(nameof(field));
            return FieldGetSymbol.Create(Destination, field.IsStatic ? null : this, field);
        }

        public void Set(string name, Symbol value)
        {
            var field = Type.GetField(name);
            if (field != null)
            {
                Destination.Put(ILExpressionNode.StoreField(field.IsStatic ? null : Take(this, field.DeclaringType!), field, Take(value, field.FieldType)));
                return;
            }
            var property = Type.GetProperty(name) ??
                throw new ArgumentException($"No field or property found '{Type.Name}.{name}'");
            var setter = property.GetSetMethod() ??
                throw new ArgumentException($"No public set accessor found for property '{Type.Name}.{property.Name}'");
            Destination.Put(ILExpressionNode.Call(setter.IsStatic ? null : Take(this, property.DeclaringType!), setter, false, Take(value, property.PropertyType!)));
        }

        public void Set(FieldInfo field, Symbol value)
        {
            Destination.Put(ILExpressionNode.StoreField(field.IsStatic ? null : Take(this, field.DeclaringType!), field, Take(value, field.FieldType)));
        }

        public Symbol Operation(UnaryOperator op) => UnaryOperationSymbol.Create(Destination, op, this);
        public Symbol Operation(BinaryOperator op, Symbol operand) => BinaryOperationSymbol.Create(Destination, op, this, operand);

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
