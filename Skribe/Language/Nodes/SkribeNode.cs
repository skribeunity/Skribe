using Skribe.Language.Memory;

namespace Skribe.Language.Nodes
{
    public abstract class ScribeNode
    {
        public abstract object Execute(ScribeContext context);
    }
}