using System.Collections.Generic;

namespace Skribe.Language.Memory
{
    public class ScribeContext
    {
        private Dictionary<string, ScribeVariable> _variables = new();
        private ScribeContext _parent;

        public ScribeContext(ScribeContext parent = null)
        {
            _parent = parent;
        }

        public ScribeVariable GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var variable))
                return variable;
            if (_parent != null)
                return _parent.GetVariable(name);
            return ScribeEngine.Instance.GetGlobalVariable(name);
        }

        public void SetVariable(string name, object value)
        {
            if (_variables.TryGetValue(name, out var variable))
                variable.Value = value;
            else
                _variables[name] = new ScribeVariable(name, value);
        }

        public ScribeContext CreateChildContext()
        {
            return new ScribeContext(this);
        }
    }
}