using System;
using System.Linq;

namespace Skribe.Language.Memory
{
    public class ScribeFunction
    {
        public string Name { get; }
        public Func<object[], object> Implementation { get; }
        public ScribeParameter[] Parameters { get; }

        public ScribeFunction(string name, Func<object[], object> implementation, ScribeParameter[] parameters)
        {
            Name = name;
            Implementation = implementation;
            Parameters = parameters;
        }

        public object Invoke(object[] args)
        {
            var required = Parameters.Count(p => !p.IsOptional);
            if (args.Length < required)
                throw new Exception($"Function '{Name}' requires at least {required} arguments, but got {args.Length}");
            if (args.Length > Parameters.Length)
                throw new Exception(
                    $"Function '{Name}' accepts at most {Parameters.Length} arguments, but got {args.Length}");

            if (args.Length < Parameters.Length)
            {
                var newArgs = new object[Parameters.Length];
                Array.Copy(args, newArgs, args.Length);
                for (var i = args.Length; i < Parameters.Length; i++)
                    newArgs[i] = Parameters[i].DefaultValue;
                args = newArgs;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var param = Parameters[i];
                var type = ScribeEngine.Instance.GetType(param.TypeName);
                if (type == null)
                    throw new Exception($"Unknown type '{param.TypeName}' for parameter '{param.Name}'");
                try
                {
                    args[i] = type.Convert(args[i]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot convert argument '{args[i]}' to '{param.TypeName}': {ex.Message}");
                }
            }

            return Implementation(args);
        }
    }
}