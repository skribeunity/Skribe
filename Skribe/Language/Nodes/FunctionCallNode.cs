using System;
using System.Collections.Generic;
using System.Linq;
using Skribe.Language.Memory;

namespace Skribe.Language.Nodes
{
    public class FunctionCallNode : ScribeNode
    {
        public string Name { get; }
        public List<ScribeNode> Arguments { get; }

        public FunctionCallNode(string name, List<ScribeNode> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public override object Execute(ScribeContext context)
        {
            var func = SkribeEngine.Instance.GetFunction(Name);
            if (func == null) throw new Exception($"Function '{Name}' not found");
            var args = Arguments.Select(a => a.Execute(context)).ToArray();
            return func.Invoke(args);
        }
    }
}