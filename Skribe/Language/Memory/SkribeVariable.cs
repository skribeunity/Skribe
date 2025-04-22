namespace Skribe.Language.Memory
{
    public class ScribeVariable
    {
        public string Name { get; }
        public object Value { get; set; }

        public ScribeVariable(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}