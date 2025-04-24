using Skribe.Language.Memory;
using Skribe.Language.Nodes;

namespace Skribe.Language
{
    /// <summary>
    /// Executes a parsed script
    /// </summary>
    public class SkribeExecutor
    {
        public object ExecuteScript(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            return script.Root.Execute(context);
        }
    }
}