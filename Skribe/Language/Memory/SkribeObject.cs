using System;

namespace Skribe.Language.Memory
{
    public class ScribeType
    {
        public string Name { get; }
        public Type DotNetType { get; }
        public Func<string, object> FromStringLiteral { get; }

        public ScribeType(string name, Type dotNetType, Func<string, object> fromStringLiteral = null)
        {
            Name = name;
            DotNetType = dotNetType;

            if (fromStringLiteral == null)
            {
                if (dotNetType == typeof(string)) FromStringLiteral = s => s;
                else if (dotNetType == typeof(double)) FromStringLiteral = s => double.Parse(s);
                else if (dotNetType == typeof(bool)) FromStringLiteral = s => bool.Parse(s);
                else FromStringLiteral = s => System.Convert.ChangeType(s, dotNetType);
            }
            else
            {
                FromStringLiteral = fromStringLiteral;
            }
        }

        public object Convert(object value)
        {
            if (value == null) return null;
            if (DotNetType.IsInstanceOfType(value)) return value;
            if (value is string s) return FromStringLiteral(s);
            if (DotNetType == typeof(double) && value is IConvertible)
                return System.Convert.ToDouble(value);
            return System.Convert.ChangeType(value, DotNetType);
        }
    }
}