using System;
using System.Collections.Generic;
using Skribe.Language.Memory;
using Skribe.Language.Parsing;
using Skribe.Language.Types;

namespace Skribe.Language
{
    /// <summary>
    /// The core Scribe language system
    /// </summary>
    public class SkribeEngine
    {
        private static SkribeEngine _instance;
        private Dictionary<string, ScribeFunction> _functions = new();
        private Dictionary<string, ScribeType> _types = new();
        private Dictionary<string, ScribeEvent> _events = new();
        private Dictionary<string, ScribeVariable> _globalVariables = new();

        /// <summary>
        /// Singleton instance of the Scribe engine
        /// </summary>
        public static SkribeEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SkribeEngine();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private SkribeEngine()
        {
            // Register basic types
            RegisterType(new ScribeType("object", typeof(object)));
            RegisterType(new ScribeType("number", typeof(double)));
            RegisterType(new ScribeType("text", typeof(string)));
            RegisterType(new ScribeType("boolean", typeof(bool)));
            RegisterType(new ScribeType("list", typeof(List<object>)));

            // Also register Vector3 globally so extensions work without explicit game‐type setup
            RegisterType(new ScribeType("Vector3", typeof(Vector3)));

            // Register basic functions
            RegisterFunction(new ScribeFunction("print", args =>
            {
                GlobalHooks.LoggingHook.Error(args[0]?.ToString() ?? "null");
                return null;
            }, new[] { new ScribeParameter("message", "text") }));

            RegisterFunction(new ScribeFunction("random", args =>
            {
                var min = Convert.ToDouble(args[0]);
                var max = Convert.ToDouble(args[1]);
                return new Random().NextDouble() * (max - min) + min;
            }, new[]
            {
                new ScribeParameter("min", "number"),
                new ScribeParameter("max", "number")
            }));
        }

        public void Flush()
        {
            foreach (var keyValuePair in _events)
            {
                keyValuePair.Value._handlers.Clear();
            }
        }

        public void RegisterType(ScribeType type)
        {
            _types[type.Name] = type;
        }

        public void RegisterFunction(ScribeFunction function)
        {
            _functions[function.Name] = function;
        }

        public void RegisterEvent(ScribeEvent scribeEvent)
        {
            _events[scribeEvent.Name] = scribeEvent;
        }

        /// <summary>
        /// Executes a Scribe script
        /// </summary>
        public object Execute(string scriptText, ScribeContext context = null)
        {
            context = context ?? new ScribeContext();

            // Parse the script
            var parser = new SkribeParser();
            var script = parser.Parse(scriptText);

            // Execute the script
            var executor = new SkribeExecutor();
            return executor.ExecuteScript(script, context);
        }

        public ScribeFunction GetFunction(string name)
        {
            _functions.TryGetValue(name, out var function);
            return function;
        }

        public ScribeType GetType(string name)
        {
            _types.TryGetValue(name, out var type);
            return type;
        }

        public ScribeEvent GetEvent(string name)
        {
            _events.TryGetValue(name, out var scribeEvent);
            return scribeEvent;
        }

        public void TriggerEvent(string eventName, params object[] parameters)
        {
            if (_events.TryGetValue(eventName, out var scribeEvent))
                scribeEvent.Trigger(parameters);
        }

        public ScribeVariable GetGlobalVariable(string name)
        {
            _globalVariables.TryGetValue(name, out var variable);
            return variable;
        }

        public void SetGlobalVariable(string name, object value)
        {
            if (_globalVariables.TryGetValue(name, out var variable))
                variable.Value = value;
            else
                _globalVariables[name] = new ScribeVariable(name, value);
        }
    }
}