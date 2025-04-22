using System;
using System.Collections.Generic;
using System.Linq;
using Skribe.Language.Enum;

namespace Skribe.Language.Parsing
{
    public class ScribeLexer
    {
        private string _source;
        private int _position, _line, _column;
        private char _current => _position < _source.Length ? _source[_position] : '\0';

        private static readonly HashSet<string> _keywords = new()
        {
            "if", "else", "while", "for", "function", "return",
            "true", "false", "null", "and", "or", "not",
            "var", "event", "on", "trigger"
        };

        public List<Token> Tokenize(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
            _column = 1;
            var tokens = new List<Token>();

            while (_position < _source.Length)
            {
                if (char.IsWhiteSpace(_current))
                {
                    if (_current == '\n')
                    {
                        tokens.Add(new Token(TokenType.EndOfLine, "\n", _line, _column));
                        _line++;
                        _column = 0;
                    }

                    _position++;
                    _column++;
                    continue;
                }

                if (_current == '#')
                {
                    while (_position < _source.Length && _current != '\n')
                    {
                        _position++;
                        _column++;
                    }

                    continue;
                }

                // identifiers / keywords (allow dots for property access)
                if (char.IsLetter(_current) || _current == '_')
                {
                    var start = _position;
                    var col = _column;
                    while (_position < _source.Length &&
                           (char.IsLetterOrDigit(_current) || _current == '_' || _current == '.'))
                    {
                        _position++;
                        _column++;
                    }

                    var value = _source.Substring(start, _position - start);
                    var type = _keywords.Contains(value) ? TokenType.Keyword : TokenType.Identifier;
                    tokens.Add(new Token(type, value, _line, col));
                    continue;
                }

                // numbers — now parsed as double for consistency
                if (char.IsDigit(_current))
                {
                    var start = _position;
                    var col = _column;
                    var hasDot = false;
                    while (_position < _source.Length &&
                           (char.IsDigit(_current) || (_current == '.' && !hasDot)))
                    {
                        if (_current == '.') hasDot = true;
                        _position++;
                        _column++;
                    }

                    var value = _source.Substring(start, _position - start);
                    tokens.Add(new Token(TokenType.Number, value, _line, col));
                    continue;
                }

                // strings
                if (_current == '"' || _current == '\'')
                {
                    var quote = _current;
                    var start = _position;
                    var col = _column;
                    _position++;
                    _column++;
                    while (_position < _source.Length && _current != quote)
                        if (_current == '\\' && _position + 1 < _source.Length)
                        {
                            _position += 2;
                            _column += 2;
                        }
                        else
                        {
                            _position++;
                            _column++;
                        }

                    if (_position >= _source.Length)
                        throw new Exception($"Unterminated string at {_line}:{col}");
                    _position++;
                    _column++;
                    var raw = _source.Substring(start, _position - start);
                    tokens.Add(new Token(TokenType.String, raw, _line, col));
                    continue;
                }

                // punctuation / operators
                switch (_current)
                {
                    case '(': tokens.Add(new Token(TokenType.LeftParen, "(", _line, _column)); break;
                    case ')': tokens.Add(new Token(TokenType.RightParen, ")", _line, _column)); break;
                    case '{': tokens.Add(new Token(TokenType.LeftBrace, "{", _line, _column)); break;
                    case '}': tokens.Add(new Token(TokenType.RightBrace, "}", _line, _column)); break;
                    case '[': tokens.Add(new Token(TokenType.LeftBracket, "[", _line, _column)); break;
                    case ']': tokens.Add(new Token(TokenType.RightBracket, "]", _line, _column)); break;
                    case ',': tokens.Add(new Token(TokenType.Comma, ",", _line, _column)); break;
                    case ':': tokens.Add(new Token(TokenType.Colon, ":", _line, _column)); break;
                    case ';':
                        tokens.Add(new Token(TokenType.EndOfLine, ";", _line, _column));
                        break;
                    default:
                        if ("+-*/=<>!&|%^".Contains(_current))
                        {
                            var start = _position;
                            var col = _column;
                            while (_position < _source.Length && "+-*/=<>!&|%^".Contains(_current))
                            {
                                _position++;
                                _column++;
                            }

                            var op = _source.Substring(start, _position - start);
                            tokens.Add(new Token(TokenType.Operator, op, _line, col));
                            continue; // already advanced
                        }
                        else
                        {
                            throw new Exception($"Unexpected character '{_current}' at {_line}:{_column}");
                        }
                }

                _position++;
                _column++;
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", _line, _column));
            return tokens;
        }
    }
}