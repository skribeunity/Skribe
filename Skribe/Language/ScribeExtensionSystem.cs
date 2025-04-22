using System;
using System.Reflection;
using Skribe.Language.Memory;
using Skribe.Language.Misc;

namespace Skribe.Language
{
    public class ScribeExtensionSystem
    {
        public static void LoadExtensionsFromAssembly(Assembly assembly)
        {
            var engine = ScribeEngine.Instance;
            // inspect both the test assembly and core Scribe assembly
            var assemblies = new[] { assembly, typeof(ScribeExtensionSystem).Assembly };
            foreach (var asm in assemblies)
            foreach (var t in asm.GetTypes())
                if (t.GetCustomAttribute<ScribeExtensionAttribute>() != null)
                    RegisterExtensionType(engine, t);
        }

        private static void RegisterExtensionType(ScribeEngine engine, Type type)
        {
            var ta = type.GetCustomAttribute<ScribeTypeAttribute>();
            if (ta != null)
                engine.RegisterType(new ScribeType(ta.Name ?? type.Name, type));

            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var fa = m.GetCustomAttribute<ScribeFunctionAttribute>();
                if (fa != null)
                    RegisterExtensionFunction(engine, m, fa);
            }

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var ea = f.GetCustomAttribute<ScribeEventAttribute>();
                if (ea != null && f.FieldType == typeof(ScribeEvent))
                {
                    var ev = (ScribeEvent)f.GetValue(null);
                    if (ev != null) engine.RegisterEvent(ev);
                }
            }
        }

        private static void RegisterExtensionFunction(ScribeEngine engine, MethodInfo m, ScribeFunctionAttribute attr)
        {
            var pars = m.GetParameters();
            var sps = new ScribeParameter[pars.Length];
            for (var i = 0; i < pars.Length; i++)
            {
                var p = pars[i];
                var tn = GetScribeTypeName(p.ParameterType);
                // auto‑register any parameter types not already known
                if (engine.GetType(tn) == null)
                    engine.RegisterType(new ScribeType(tn, p.ParameterType));

                if (p.IsOptional)
                    sps[i] = new ScribeParameter(p.Name, tn, p.DefaultValue);
                else
                    sps[i] = new ScribeParameter(p.Name, tn);
            }

            var func = new ScribeFunction(attr.Name ?? m.Name, args => m.Invoke(null, args), sps);
            engine.RegisterFunction(func);
        }

        private static string GetScribeTypeName(Type type)
        {
            if (type == typeof(double) || type == typeof(int) || type == typeof(float)) return "number";
            if (type == typeof(string)) return "text";
            if (type == typeof(bool)) return "boolean";
            var ta = type.GetCustomAttribute<ScribeTypeAttribute>();
            return ta?.Name ?? type.Name;
        }
    }
}