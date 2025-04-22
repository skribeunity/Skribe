using System;
using System.Linq;
using System.Reflection;
using Skribe.Language.Memory;

namespace Skribe.Language.Nodes
{
    public class AssignmentNode : ScribeNode
    {
        public string Name { get; }
        public ScribeNode Value { get; }

        public AssignmentNode(string name, ScribeNode value)
        {
            Name = name;
            Value = value;
        }

        public override object Execute(ScribeContext context)
        {
            var val = Value.Execute(context);

            // handle dotted assignment (property/field write)
            if (Name.Contains("."))
            {
                var parts = Name.Split('.');
                var baseVar = context.GetVariable(parts[0]);
                if (baseVar == null)
                    throw new Exception($"Variable '{parts[0]}' not found for assignment");
                var obj = baseVar.Value;

                // drill down to the owner of the last member
                for (var i = 1; i < parts.Length - 1; i++)
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

                var last = parts.Last();
                var finalType = obj.GetType();
                var finalProp = finalType.GetProperty(last, BindingFlags.Public | BindingFlags.Instance);
                if (finalProp != null)
                    finalProp.SetValue(obj, val);
                else if (finalType.GetField(last, BindingFlags.Public | BindingFlags.Instance) is FieldInfo finalField)
                    finalField.SetValue(obj, val);
                else
                    throw new Exception($"No member '{last}' on {finalType.Name} to assign");

                return val;
            }

            // simple variable assignment
            context.SetVariable(Name, val);
            return val;
        }
    }
}