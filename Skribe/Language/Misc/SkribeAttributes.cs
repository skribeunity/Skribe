using System;

namespace Skribe.Language.Misc
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ScribeExtensionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ScribeTypeAttribute : Attribute
    {
        public string Name { get; }

        public ScribeTypeAttribute(string name = null)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ScribeFunctionAttribute : Attribute
    {
        public string Name { get; }

        public ScribeFunctionAttribute(string name = null)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ScribeEventAttribute : Attribute
    {
    }
}