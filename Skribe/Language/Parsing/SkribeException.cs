// ScribeDiagnostics.cs
using System;
using Skribe.Language.Enum;

namespace Skribe.Language.Parsing
{
    /// <summary>Base for any problem detected while compiling Scribe.</summary>
    public abstract class ScribeException : Exception
    {
        public int   Line   { get; }
        public int   Column { get; }

        protected ScribeException(string message, Token token)
            : base($"{message} (line {token?.Line}, col {token?.Column})")
        {
            Line   = token?.Line   ?? 0;
            Column = token?.Column ?? 0;
        }
    }

    public sealed class LexicalException : ScribeException
    {
        public LexicalException(string message, Token token)
            : base(message, token) { }
    }

    public sealed class SyntaxException : ScribeException
    {
        public SyntaxException(string message, Token token)
            : base(message, token) { }
    }
}