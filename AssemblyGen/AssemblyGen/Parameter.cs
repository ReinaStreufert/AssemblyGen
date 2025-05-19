namespace AssemblyGen
{
    public class Parameter
    {
        public Type Type { get; }

        public Parameter(Type type)
        {
            Type = type;
        }
    }

    public static class TypeExtensions
    {
        public static Parameter AsParameter(this Type type) => new Parameter(type);
    }
}