using System;

namespace Skribe.Language.Misc
{
    /// <summary>
    /// Thrown by ReturnNode to unwind out of the current function body.
    /// </summary>
    public class ReturnException : Exception
    {
        public object Value { get; }

        public ReturnException(object value)
        {
            Value = value;
        }
    }
}