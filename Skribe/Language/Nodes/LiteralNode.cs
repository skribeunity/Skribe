using Skribe.Language.Memory;

namespace Skribe.Language.Nodes
{
    public class LiteralNode : ScribeNode
    {
        public object Value { get; }

        public LiteralNode(object value)
        {
            Value = value;
        }

        public override object Execute(ScribeContext context)
        {
            return Value;
        }
    }
}