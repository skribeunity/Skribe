using System;
using System.Collections.Generic;
using Skribe.Language.Nodes;

namespace Skribe.Language.Memory
{
    public class ScribeEvent
    {
        public string Name { get; }
        public ScribeParameter[] Parameters { get; }

        // Now store both script + the context it was registered in
        internal readonly List<(BinaryOpNode.ScribeScript Script, ScribeContext Context)> _handlers = new();

        public ScribeEvent(string name, ScribeParameter[] parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        // Register both the script and its defining context
        public void RegisterHandler(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            _handlers.Add((script, context));
        }

        public void UnregisterHandler(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            _handlers.RemoveAll(h => h.Script == script && h.Context == context);
        }

        public void Trigger(object[] args)
        {
            if (args.Length != Parameters.Length)
                throw new Exception($"Event '{Name}' requires {Parameters.Length} parameters, but got {args.Length}");

            var executor = new SkribeExecutor();

            foreach (var (script, ctx) in _handlers)
                try
                {
                    // inject each parameter into the *registration* context
                    for (var i = 0; i < args.Length; i++)
                        ctx.SetVariable(Parameters[i].Name, args[i]);

                    // run the handler in *that* context
                    executor.ExecuteScript(script, ctx);
                }
                catch (Exception ex)
                {
                    GlobalHooks.LoggingHook.Error($"Error in handler for '{Name}': {ex.Message}");
                }
        }
    }
}