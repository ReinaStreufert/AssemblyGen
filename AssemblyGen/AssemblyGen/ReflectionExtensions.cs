using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    internal static class ReflectionExtensions
    {
        public static MethodInfo? MatchMethod(this Type type, string methodName, params Type[] argumentTypes)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => m.Name == methodName)
                .Select(m => TryMatchMethod(m, argumentTypes))
                .Where(m => m != null)
                .FirstOrDefault();
        }

        public static ConstructorInfo? MatchConstructor(this Type type, params Type[] argumentTypes)
        {
            return type.GetConstructors(BindingFlags.Public)
                .Where(c => c.IsMatch(argumentTypes))
                .FirstOrDefault();
        }

        public static MethodInfo? TryMatchMethod(this MethodInfo method, params Type[] argumentTypes)
        {
            var genericDict = method.ContainsGenericParameters ? new Dictionary<Type, Type>() : null;
            var methodParameters = method.GetParameters();
            if (argumentTypes.Length > methodParameters.Length)
                return null;
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                var matchType = i < argumentTypes.Length ? argumentTypes[i] : null;
                if (matchType == null)
                {
                    if (parameter.HasDefaultValue)
                        continue;
                    else
                        return null;
                }
                var parameterType = parameter.ParameterType;
                if (parameterType.IsGenericType)
                {
                    if (genericDict!.TryGetValue(parameter.ParameterType, out var genericArg))
                    {
                        var commonAncestor = TryFindCommonAncestor(genericArg, matchType);
                        if (commonAncestor == null)
                            return null;
                        genericDict[parameterType] = commonAncestor;
                    }
                    else if (parameterType.GetGenericParameterConstraints().All(matchType.IsAssignableTo))
                        genericDict[parameterType] = matchType;
                    else return null;
                } else if (!matchType.IsAssignableTo(parameter.ParameterType))
                    return null;
            }
            return method;
        }

        public static bool IsMatch(this ConstructorInfo constructor, params Type[] argumentTypes)
        {
            var parameters = constructor.GetParameters();
            if (argumentTypes.Length > parameters.Length)
                return false;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (i < argumentTypes.Length)
                {
                    if (!argumentTypes[i].IsAssignableTo(parameter.ParameterType))
                        return false;
                }
                else return parameter.HasDefaultValue;
            }
            return true;
        }

        private static Type? TryFindCommonAncestor(this Type a, Type b)
        {
            for (Type? currentAncestor = a; currentAncestor != null; currentAncestor = currentAncestor.BaseType)
            {
                if (b.IsAssignableTo(currentAncestor))
                    return currentAncestor;
            }
            return null;
        }
    }
}
