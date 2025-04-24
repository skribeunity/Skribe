using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Skribe.Language.Enum;
using Skribe.Language.Nodes;

namespace Skribe.Language.Parsing
{
    /// <summary>
    /// Parser for the Scribe language
    /// </summary>
    public class SkribeParser
    {
        private List<Token> _tokens;
        private int _position;
        private Token _current => _position < _tokens.Count ? _tokens[_position] : null;

        public BinaryOpNode.ScribeScript Parse(string sourceUnprocessed)
        {
            var source = new Preprocessor().Process(sourceUnprocessed);
            var lexer = new ScribeLexer();
            _tokens = lexer.Tokenize(source);
            _position = 0;

            var statements = new List<ScribeNode>();

            // skip leading blank lines/semicolons
            while (_current?.Type == TokenType.EndOfLine)
                _position++;

            // parse until EOF
            while (_current?.Type != TokenType.EndOfFile)
            {
                statements.Add(ParseStatement());

                // skip blank lines/semicolons
                while (_current?.Type == TokenType.EndOfLine)
                    _position++;
            }

            return new BinaryOpNode.ScribeScript { Root = new BinaryOpNode.BlockNode(statements) };
        }

        private ScribeNode ParseStatement()
        {
            if (_current.Type == TokenType.Keyword)
                switch (_current.Value)
                {
                    case "if": return ParseIfStatement();
                    case "while": return ParseWhileStatement();
                    case "for": return ParseForStatement();
                    case "function": return ParseFunctionDefinition();
                    case "return": return ParseReturnStatement();
                    case "var": return ParseVariableDeclaration();
                    case "on": return ParseEventHandler();
                }

            if (_current.Type == TokenType.LeftBrace)
                return ParseBlock();

            return ParseExpressionStatement();
        }

        private ScribeNode ParseIfStatement()
        {
            Expect(TokenType.Keyword, "if");
            Expect(TokenType.LeftParen);
            var cond = ParseExpression();
            Expect(TokenType.RightParen);
            var thenB = ParseStatement();
            ScribeNode elseB = null;
            if (_current?.Type == TokenType.Keyword && _current.Value == "else")
            {
                _position++;
                elseB = ParseStatement();
            }

            return new BinaryOpNode.IfNode(cond, thenB, elseB);
        }

        private ScribeNode ParseWhileStatement()
        {
            Expect(TokenType.Keyword, "while");
            Expect(TokenType.LeftParen);
            var cond = ParseExpression();
            Expect(TokenType.RightParen);
            var body = ParseStatement();
            return new BinaryOpNode.WhileNode(cond, body);
        }

        private ScribeNode ParseForStatement()
        {
            Expect(TokenType.Keyword, "for");
            Expect(TokenType.LeftParen);

            // initialization: allow 'var'
            ScribeNode init;
            if (_current.Type == TokenType.Keyword && _current.Value == "var")
            {
                init = ParseVariableDeclaration();
                Expect(TokenType.EndOfLine); // semicolon
            }
            else
            {
                init = ParseExpressionStatement();
            }

            var cond = ParseExpression();
            Expect(TokenType.EndOfLine); // semicolon
            var incr = ParseExpression();
            Expect(TokenType.RightParen);
            var body = ParseStatement();
            return new BinaryOpNode.ForNode(init, cond, incr, body);
        }

        private ScribeNode ParseFunctionDefinition()
        {
            Expect(TokenType.Keyword, "function");
            var name = Expect(TokenType.Identifier).Value;
            Expect(TokenType.LeftParen);
            var parms = new List<string>();
            if (_current.Type != TokenType.RightParen)
                do
                {
                    parms.Add(Expect(TokenType.Identifier).Value);
                    if (_current.Type == TokenType.Comma) _position++;
                    else break;
                } while (true);

            Expect(TokenType.RightParen);
            var body = ParseStatement();
            return new BinaryOpNode.FunctionDefNode(name, parms, body);
        }

        private ScribeNode ParseReturnStatement()
        {
            Expect(TokenType.Keyword, "return");
            ScribeNode value;
            if (_current.Type == TokenType.EndOfLine)
                value = new LiteralNode(null);
            else
                value = ParseExpression();
            return new BinaryOpNode.ReturnNode(value);
        }

        private ScribeNode ParseVariableDeclaration()
        {
            Expect(TokenType.Keyword, "var");
            var name = Expect(TokenType.Identifier).Value;
            ScribeNode val;
            if (_current.Type == TokenType.Operator && _current.Value == "=")
            {
                _position++;
                val = ParseExpression();
            }
            else
            {
                val = new LiteralNode(null);
            }

            return new AssignmentNode(name, val);
        }

        private ScribeNode ParseEventHandler()
        {
            Expect(TokenType.Keyword, "on");
            var ev = Expect(TokenType.Identifier).Value;
            var body = ParseStatement();
            return new BinaryOpNode.EventHandlerNode(ev, body);
        }

        private ScribeNode ParseBlock()
        {
            Expect(TokenType.LeftBrace);
            var stmts = new List<ScribeNode>();

            // skip leading blank lines
            while (_current.Type == TokenType.EndOfLine)
                _position++;

            while (_current.Type != TokenType.RightBrace && _current.Type != TokenType.EndOfFile)
            {
                stmts.Add(ParseStatement());
                while (_current?.Type == TokenType.EndOfLine)
                    _position++;
            }

            Expect(TokenType.RightBrace);
            return new BinaryOpNode.BlockNode(stmts);
        }

        private ScribeNode ParseExpressionStatement()
        {
            var expr = ParseExpression();
            if (_current.Type == TokenType.EndOfLine)
                _position++;
            return expr;
        }

        private ScribeNode ParseExpression()
        {
            return ParseAssignment();
        }

        private ScribeNode ParseAssignment()
        {
            var expr = ParseLogicalOr();
            if (_current.Type == TokenType.Operator && _current.Value == "=")
            {
                _position++;
                var value = ParseAssignment();
                if (expr is VariableNode varNode)
                    return new AssignmentNode(varNode.Name, value);
                throw new Exception("Invalid assignment target");
            }

            return expr;
        }

        private ScribeNode ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();
            while (_current.Type == TokenType.Keyword && _current.Value == "or")
            {
                var op = _current.Value;
                _position++;
                var right = ParseLogicalAnd();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseLogicalAnd()
        {
            var expr = ParseEquality();
            while (_current.Type == TokenType.Keyword && _current.Value == "and")
            {
                var op = _current.Value;
                _position++;
                var right = ParseEquality();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseEquality()
        {
            var expr = ParseComparison();
            while (_current.Type == TokenType.Operator &&
                   (_current.Value == "==" || _current.Value == "!="))
            {
                var op = _current.Value;
                _position++;
                var right = ParseComparison();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseComparison()
        {
            var expr = ParseAddition();
            while (_current.Type == TokenType.Operator &&
                   (_current.Value == "<" || _current.Value == ">" ||
                    _current.Value == "<=" || _current.Value == ">="))
            {
                var op = _current.Value;
                _position++;
                var right = ParseAddition();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseAddition()
        {
            var expr = ParseMultiplication();
            while (_current.Type == TokenType.Operator &&
                   (_current.Value == "+" || _current.Value == "-"))
            {
                var op = _current.Value;
                _position++;
                var right = ParseMultiplication();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseMultiplication()
        {
            var expr = ParseUnary();
            while (_current.Type == TokenType.Operator &&
                   (_current.Value == "*" || _current.Value == "/" || _current.Value == "%"))
            {
                var op = _current.Value;
                _position++;
                var right = ParseUnary();
                expr = new BinaryOpNode(expr, op, right);
            }

            return expr;
        }

        private ScribeNode ParseUnary()
        {
            if (_current.Type == TokenType.Operator &&
                (_current.Value == "-" || _current.Value == "!"))
            {
                var op = _current.Value;
                _position++;
                var right = ParseUnary();
                return new BinaryOpNode.UnaryOpNode(op, right);
            }

            if (_current.Type == TokenType.Keyword && _current.Value == "not")
            {
                _position++;
                var right = ParseUnary();
                return new BinaryOpNode.UnaryOpNode("not", right);
            }

            return ParsePrimary();
        }

        private ScribeNode ParsePrimary()
        {
            if (_current.Type == TokenType.Number)
            {
                // now using double for all literals
                var val = double.Parse(_current.Value);
                _position++;
                return new LiteralNode(val);
            }

            if (_current.Type == TokenType.String)
            {
                var raw = _current.Value;
                _position++;
                var s = raw.Substring(1, raw.Length - 2);
                s = Regex.Unescape(s);
                return new LiteralNode(s);
            }

            if (_current.Type == TokenType.Keyword)
                switch (_current.Value)
                {
                    case "true":
                        _position++;
                        return new LiteralNode(true);
                    case "false":
                        _position++;
                        return new LiteralNode(false);
                    case "null":
                        _position++;
                        return new LiteralNode(null);
                }

            if (_current.Type == TokenType.Identifier)
            {
                var name = _current.Value;
                _position++;
                if (_current.Type == TokenType.LeftParen)
                {
                    _position++;
                    var args = new List<ScribeNode>();
                    if (_current.Type != TokenType.RightParen)
                        do
                        {
                            args.Add(ParseExpression());
                            if (_current.Type == TokenType.Comma) _position++;
                            else break;
                        } while (true);

                    Expect(TokenType.RightParen);
                    return new FunctionCallNode(name, args);
                }

                return new VariableNode(name);
            }

            if (_current.Type == TokenType.LeftParen)
            {
                _position++;
                var expr = ParseExpression();
                Expect(TokenType.RightParen);
                return expr;
            }

            throw new Exception($"Unexpected token: {_current}");
        }

        private Token Expect(TokenType type, string value = null)
        {
            // ------------------------------------------------------------------
            // 1) End-of-file guard
            // ------------------------------------------------------------------
            if (_current == null)
            {
                // use the last token that actually exists, or a dummy one
                Token lastToken = _tokens.Count > 0
                    ? _tokens[_tokens.Count - 1]
                    : new Token(TokenType.EndOfFile, string.Empty, 0, 0);

                throw new SyntaxException("Unexpected end of file", lastToken);
            }

            // ------------------------------------------------------------------
            // 2) Type / value mismatch
            // ------------------------------------------------------------------
            bool typeMismatch  = _current.Type != type;
            bool valueMismatch = value != null && _current.Value != value;

            if (typeMismatch || valueMismatch)
            {
                string expected = value ?? type.ToString();
                throw new SyntaxException(
                    $"Expected {expected}, found '{_current.Value}'",
                    _current);
            }

            // ------------------------------------------------------------------
            // 3) Success – return token and advance
            // ------------------------------------------------------------------
            Token tok = _current;
            _position++;
            return tok;
        }
    }
}