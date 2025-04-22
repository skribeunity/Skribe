namespace Skribe.Language.Memory
{
    public class ScribeParameter
    {
        public string Name { get; }
        public string TypeName { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        public ScribeParameter(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
            IsOptional = false;
        }

        public ScribeParameter(string name, string typeName, object defaultValue)
        {
            Name = name;
            TypeName = typeName;
            IsOptional = true;
            DefaultValue = defaultValue;
        }
    }
}