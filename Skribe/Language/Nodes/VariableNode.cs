using System;
using System.Reflection;
using Skribe.Language.Memory;

namespace Skribe.Language.Nodes
{
    public class VariableNode : ScribeNode
    {
        public string Name { get; }

        public VariableNode(string name)
        {
            Name = name;
        }

        public override object Execute(ScribeContext context)
        {
            // dotted lookup
            if (Name.Contains("."))
            {
                var parts = Name.Split('.');
                var var0 = context.GetVariable(parts[0]);
                if (var0 == null)
                    throw new Exception($"Variable '{parts[0]}' not found");
                var obj = var0.Value;

                for (var i = 1; i < parts.Length; i++)
                {
                    if (obj == null)
                        throw new Exception($"Cannot access '{parts[i]}' on null");

                    var t = obj.GetType();
                    var prop = t.GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                        obj = prop.GetValue(obj);
                    else if (t.GetField(parts[i], BindingFlags.Public | BindingFlags.Instance) is FieldInfo f)
                        obj = f.GetValue(obj);
                    else
                        throw new Exception($"No member '{parts[i]}' on {t.Name}");
                }

                return obj;
            }

            var variable = context.GetVariable(Name);
            if (variable == null)
                throw new Exception($"Variable '{Name}' not found");
            return variable.Value;
        }
    }
}