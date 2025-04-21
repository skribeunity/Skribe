using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Skribe.Language
{
    /// <summary>
    /// The core Scribe language system
    /// </summary>
    public class ScribeEngine
    {
        private static ScribeEngine _instance;
        private Dictionary<string, ScribeFunction> _functions = new();
        private Dictionary<string, ScribeType> _types = new();
        private Dictionary<string, ScribeEvent> _events = new();
        private Dictionary<string, ScribeVariable> _globalVariables = new();

        /// <summary>
        /// Singleton instance of the Scribe engine
        /// </summary>
        public static ScribeEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ScribeEngine();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private ScribeEngine()
        {
            // Register basic types
            RegisterType(new ScribeType("object", typeof(object)));
            RegisterType(new ScribeType("number", typeof(double)));
            RegisterType(new ScribeType("text", typeof(string)));
            RegisterType(new ScribeType("boolean", typeof(bool)));
            RegisterType(new ScribeType("list", typeof(List<object>)));

            // Also register Vector3 globally so extensions work without explicit game‐type setup
            RegisterType(new ScribeType("Vector3", typeof(Vector3)));

            // Register basic functions
            RegisterFunction(new ScribeFunction("print", args =>
            {
                Console.WriteLine(args[0]?.ToString() ?? "null");
                return null;
            }, new[] { new ScribeParameter("message", "text") }));

            RegisterFunction(new ScribeFunction("random", args =>
            {
                var min = Convert.ToDouble(args[0]);
                var max = Convert.ToDouble(args[1]);
                return new Random().NextDouble() * (max - min) + min;
            }, new[]
            {
                new ScribeParameter("min", "number"),
                new ScribeParameter("max", "number")
            }));
        }

        public void RegisterType(ScribeType type)
        {
            _types[type.Name] = type;
        }

        public void RegisterFunction(ScribeFunction function)
        {
            _functions[function.Name] = function;
        }

        public void RegisterEvent(ScribeEvent scribeEvent)
        {
            _events[scribeEvent.Name] = scribeEvent;
        }

        /// <summary>
        /// Executes a Scribe script
        /// </summary>
        public object Execute(string scriptText, ScribeContext context = null)
        {
            context = context ?? new ScribeContext();

            // Parse the script
            var parser = new ScribeParser();
            var script = parser.Parse(scriptText);

            // Execute the script
            var executor = new ScribeExecutor();
            return executor.ExecuteScript(script, context);
        }

        public ScribeFunction GetFunction(string name)
        {
            _functions.TryGetValue(name, out var function);
            return function;
        }

        public ScribeType GetType(string name)
        {
            _types.TryGetValue(name, out var type);
            return type;
        }

        public ScribeEvent GetEvent(string name)
        {
            _events.TryGetValue(name, out var scribeEvent);
            return scribeEvent;
        }

        public void TriggerEvent(string eventName, params object[] parameters)
        {
            if (_events.TryGetValue(eventName, out var scribeEvent))
                scribeEvent.Trigger(parameters);
        }

        public ScribeVariable GetGlobalVariable(string name)
        {
            _globalVariables.TryGetValue(name, out var variable);
            return variable;
        }

        public void SetGlobalVariable(string name, object value)
        {
            if (_globalVariables.TryGetValue(name, out var variable))
                variable.Value = value;
            else
                _globalVariables[name] = new ScribeVariable(name, value);
        }
    }

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

    public class ScribeType
    {
        public string Name { get; }
        public Type DotNetType { get; }
        public Func<string, object> FromStringLiteral { get; }

        public ScribeType(string name, Type dotNetType, Func<string, object> fromStringLiteral = null)
        {
            Name = name;
            DotNetType = dotNetType;

            if (fromStringLiteral == null)
            {
                if (dotNetType == typeof(string)) FromStringLiteral = s => s;
                else if (dotNetType == typeof(double)) FromStringLiteral = s => double.Parse(s);
                else if (dotNetType == typeof(bool)) FromStringLiteral = s => bool.Parse(s);
                else FromStringLiteral = s => System.Convert.ChangeType(s, dotNetType);
            }
            else
            {
                FromStringLiteral = fromStringLiteral;
            }
        }

        public object Convert(object value)
        {
            if (value == null) return null;
            if (DotNetType.IsInstanceOfType(value)) return value;
            if (value is string s) return FromStringLiteral(s);
            if (DotNetType == typeof(double) && value is IConvertible)
                return System.Convert.ToDouble(value);
            return System.Convert.ChangeType(value, DotNetType);
        }
    }

    public class ScribeParameter
    {
        public string Name { get; }
        public string TypeName { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        public ScribeParameter(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
            IsOptional = false;
        }

        public ScribeParameter(string name, string typeName, object defaultValue)
        {
            Name = name;
            TypeName = typeName;
            IsOptional = true;
            DefaultValue = defaultValue;
        }
    }

    public class ScribeFunction
    {
        public string Name { get; }
        public Func<object[], object> Implementation { get; }
        public ScribeParameter[] Parameters { get; }

        public ScribeFunction(string name, Func<object[], object> implementation, ScribeParameter[] parameters)
        {
            Name = name;
            Implementation = implementation;
            Parameters = parameters;
        }

        public object Invoke(object[] args)
        {
            var required = Parameters.Count(p => !p.IsOptional);
            if (args.Length < required)
                throw new Exception($"Function '{Name}' requires at least {required} arguments, but got {args.Length}");
            if (args.Length > Parameters.Length)
                throw new Exception(
                    $"Function '{Name}' accepts at most {Parameters.Length} arguments, but got {args.Length}");

            if (args.Length < Parameters.Length)
            {
                var newArgs = new object[Parameters.Length];
                Array.Copy(args, newArgs, args.Length);
                for (var i = args.Length; i < Parameters.Length; i++)
                    newArgs[i] = Parameters[i].DefaultValue;
                args = newArgs;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var param = Parameters[i];
                var type = ScribeEngine.Instance.GetType(param.TypeName);
                if (type == null)
                    throw new Exception($"Unknown type '{param.TypeName}' for parameter '{param.Name}'");
                try
                {
                    args[i] = type.Convert(args[i]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot convert argument '{args[i]}' to '{param.TypeName}': {ex.Message}");
                }
            }

            return Implementation(args);
        }
    }

    public class ScribeEvent
    {
        public string Name { get; }
        public ScribeParameter[] Parameters { get; }

        // Now store both script + the context it was registered in
        private readonly List<(BinaryOpNode.ScribeScript Script, ScribeContext Context)> _handlers = new();

        public ScribeEvent(string name, ScribeParameter[] parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        // Register both the script and its defining context
        public void RegisterHandler(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            _handlers.Add((script, context));
        }

        public void UnregisterHandler(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            _handlers.RemoveAll(h => h.Script == script && h.Context == context);
        }

        public void Trigger(object[] args)
        {
            if (args.Length != Parameters.Length)
                throw new Exception($"Event '{Name}' requires {Parameters.Length} parameters, but got {args.Length}");

            var executor = new ScribeExecutor();

            foreach (var (script, ctx) in _handlers)
                try
                {
                    // inject each parameter into the *registration* context
                    for (var i = 0; i < args.Length; i++)
                        ctx.SetVariable(Parameters[i].Name, args[i]);

                    // run the handler in *that* context
                    executor.ExecuteScript(script, ctx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in handler for '{Name}': {ex.Message}");
                }
        }
    }


    public enum TokenType
    {
        Identifier,
        Number,
        String,
        Operator,
        Keyword,
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Colon,
        EndOfLine,
        EndOfFile
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) at {Line}:{Column}";
        }
    }

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

    public abstract class ScribeNode
    {
        public abstract object Execute(ScribeContext context);
    }

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

    public class VariableNode : ScribeNode
    {
        public string Name { get; }

        public VariableNode(string name)
        {
            Name = name;
        }

        public override object Execute(ScribeContext context)
        {
            // dotted lookup
            if (Name.Contains("."))
            {
                var parts = Name.Split('.');
                var var0 = context.GetVariable(parts[0]);
                if (var0 == null)
                    throw new Exception($"Variable '{parts[0]}' not found");
                var obj = var0.Value;

                for (var i = 1; i < parts.Length; i++)
                {
                    if (obj == null)
                        throw new Exception($"Cannot access '{parts[i]}' on null");

                    var t = obj.GetType();
                    var prop = t.GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                        obj = prop.GetValue(obj);
                    else if (t.GetField(parts[i], BindingFlags.Public | BindingFlags.Instance) is FieldInfo f)
                        obj = f.GetValue(obj);
                    else
                        throw new Exception($"No member '{parts[i]}' on {t.Name}");
                }

                return obj;
            }

            var variable = context.GetVariable(Name);
            if (variable == null)
                throw new Exception($"Variable '{Name}' not found");
            return variable.Value;
        }
    }

    public class AssignmentNode : ScribeNode
    {
        public string Name { get; }
        public ScribeNode Value { get; }

        public AssignmentNode(string name, ScribeNode value)
        {
            Name = name;
            Value = value;
        }

        public override object Execute(ScribeContext context)
        {
            var val = Value.Execute(context);

            // handle dotted assignment (property/field write)
            if (Name.Contains("."))
            {
                var parts = Name.Split('.');
                var baseVar = context.GetVariable(parts[0]);
                if (baseVar == null)
                    throw new Exception($"Variable '{parts[0]}' not found for assignment");
                var obj = baseVar.Value;

                // drill down to the owner of the last member
                for (var i = 1; i < parts.Length - 1; i++)
                {
                    if (obj == null)
                        throw new Exception($"Cannot access '{parts[i]}' on null");

                    var t = obj.GetType();
                    var prop = t.GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                        obj = prop.GetValue(obj);
                    else if (t.GetField(parts[i], BindingFlags.Public | BindingFlags.Instance) is FieldInfo f)
                        obj = f.GetValue(obj);
                    else
                        throw new Exception($"No member '{parts[i]}' on {t.Name}");
                }

                var last = parts.Last();
                var finalType = obj.GetType();
                var finalProp = finalType.GetProperty(last, BindingFlags.Public | BindingFlags.Instance);
                if (finalProp != null)
                    finalProp.SetValue(obj, val);
                else if (finalType.GetField(last, BindingFlags.Public | BindingFlags.Instance) is FieldInfo finalField)
                    finalField.SetValue(obj, val);
                else
                    throw new Exception($"No member '{last}' on {finalType.Name} to assign");

                return val;
            }

            // simple variable assignment
            context.SetVariable(Name, val);
            return val;
        }
    }

    public class FunctionCallNode : ScribeNode
    {
        public string Name { get; }
        public List<ScribeNode> Arguments { get; }

        public FunctionCallNode(string name, List<ScribeNode> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public override object Execute(ScribeContext context)
        {
            var func = ScribeEngine.Instance.GetFunction(Name);
            if (func == null) throw new Exception($"Function '{Name}' not found");
            var args = Arguments.Select(a => a.Execute(context)).ToArray();
            return func.Invoke(args);
        }
    }

    public class BinaryOpNode : ScribeNode
    {
        public ScribeNode Left { get; }
        public string Operator { get; }
        public ScribeNode Right { get; }

        public BinaryOpNode(ScribeNode left, string @operator, ScribeNode right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override object Execute(ScribeContext context)
        {
            var leftVal = Left.Execute(context);

            switch (Operator)
            {
                case "and":
                    if (!Convert.ToBoolean(leftVal)) return false;
                    return Convert.ToBoolean(Right.Execute(context));
                case "or":
                    if (Convert.ToBoolean(leftVal)) return true;
                    return Convert.ToBoolean(Right.Execute(context));
            }

            var rightVal = Right.Execute(context);

            switch (Operator)
            {
                case "+":
                    if (leftVal is string || rightVal is string)
                        return leftVal?.ToString() + rightVal?.ToString();
                    return Convert.ToDouble(leftVal) + Convert.ToDouble(rightVal);
                case "-": return Convert.ToDouble(leftVal) - Convert.ToDouble(rightVal);
                case "*": return Convert.ToDouble(leftVal) * Convert.ToDouble(rightVal);
                case "/": return Convert.ToDouble(leftVal) / Convert.ToDouble(rightVal);
                case "%": return Convert.ToDouble(leftVal) % Convert.ToDouble(rightVal);
                case "==": return Equals(leftVal, rightVal);
                case "!=": return !Equals(leftVal, rightVal);
                case "<":
                    if (leftVal is IComparable c1) return c1.CompareTo(rightVal) < 0;
                    break;
                case ">":
                    if (leftVal is IComparable c2) return c2.CompareTo(rightVal) > 0;
                    break;
                case "<=":
                    if (leftVal is IComparable c3) return c3.CompareTo(rightVal) <= 0;
                    break;
                case ">=":
                    if (leftVal is IComparable c4) return c4.CompareTo(rightVal) >= 0;
                    break;
            }

            throw new Exception($"Unknown operator: {Operator}");
        }

        public class UnaryOpNode : ScribeNode
        {
            public string Operator { get; }
            public ScribeNode Operand { get; }

            public UnaryOpNode(string @operator, ScribeNode operand)
            {
                Operator = @operator;
                Operand = operand;
            }

            public override object Execute(ScribeContext context)
            {
                var val = Operand.Execute(context);
                switch (Operator)
                {
                    case "-": return -Convert.ToDouble(val);
                    case "!":
                    case "not": return !Convert.ToBoolean(val);
                }

                throw new Exception($"Unknown unary operator: {Operator}");
            }
        }

        public class BlockNode : ScribeNode
        {
            public List<ScribeNode> Statements { get; }

            public BlockNode(List<ScribeNode> statements)
            {
                Statements = statements;
            }

            public override object Execute(ScribeContext context)
            {
                object result = null;
                try
                {
                    foreach (var stmt in Statements)
                        result = stmt.Execute(context);
                }
                catch (ReturnException ret)
                {
                    // On a return, immediately yield its value.
                    return ret.Value;
                }

                return result;
            }
        }


        public class IfNode : ScribeNode
        {
            public ScribeNode Condition { get; }
            public ScribeNode ThenBranch { get; }
            public ScribeNode ElseBranch { get; }

            public IfNode(ScribeNode condition, ScribeNode thenBranch, ScribeNode elseBranch = null)
            {
                Condition = condition;
                ThenBranch = thenBranch;
                ElseBranch = elseBranch;
            }

            public override object Execute(ScribeContext context)
            {
                var cond = Convert.ToBoolean(Condition.Execute(context));
                if (cond) return ThenBranch.Execute(context);
                else if (ElseBranch != null) return ElseBranch.Execute(context);
                return null;
            }
        }

        public class WhileNode : ScribeNode
        {
            public ScribeNode Condition { get; }
            public ScribeNode Body { get; }

            public WhileNode(ScribeNode condition, ScribeNode body)
            {
                Condition = condition;
                Body = body;
            }

            public override object Execute(ScribeContext context)
            {
                object result = null;
                while (Convert.ToBoolean(Condition.Execute(context)))
                    result = Body.Execute(context);
                return result;
            }
        }

        public class ForNode : ScribeNode
        {
            public ScribeNode Initialization { get; }
            public ScribeNode Condition { get; }
            public ScribeNode Increment { get; }
            public ScribeNode Body { get; }

            public ForNode(ScribeNode initialization, ScribeNode condition, ScribeNode increment, ScribeNode body)
            {
                Initialization = initialization;
                Condition = condition;
                Increment = increment;
                Body = body;
            }

            public override object Execute(ScribeContext context)
            {
                object result = null;
                for (Initialization.Execute(context);
                     Convert.ToBoolean(Condition.Execute(context));
                     Increment.Execute(context))
                    result = Body.Execute(context);
                return result;
            }
        }

        public class FunctionDefNode : ScribeNode
        {
            public string Name { get; }
            public List<string> Parameters { get; }
            public ScribeNode Body { get; }

            public FunctionDefNode(string name, List<string> parameters, ScribeNode body)
            {
                Name = name;
                Parameters = parameters;
                Body = body;
            }

            public override object Execute(ScribeContext context)
            {
                // user‑defined parameters typed as "object"
                var sparams = Parameters.Select(p => new ScribeParameter(p, "object")).ToArray();
                var impl = new Func<object[], object>(args =>
                {
                    var fnCtx = context.CreateChildContext();
                    for (var i = 0; i < Parameters.Count; i++)
                        fnCtx.SetVariable(Parameters[i], i < args.Length ? args[i] : null);
                    return Body.Execute(fnCtx);
                });
                ScribeEngine.Instance.RegisterFunction(new ScribeFunction(Name, impl, sparams));
                return null;
            }
        }

        public class ReturnNode : ScribeNode
        {
            public ScribeNode Value { get; }

            public ReturnNode(ScribeNode value)
            {
                Value = value;
            }

            public override object Execute(ScribeContext context)
            {
                // Evaluate the return expression, then throw to escape the block.
                var val = Value.Execute(context);
                throw new ReturnException(val);
            }
        }


        public class EventHandlerNode : ScribeNode
        {
            public string EventName { get; }
            public ScribeNode Body { get; }

            public EventHandlerNode(string eventName, ScribeNode body)
            {
                EventName = eventName;
                Body = body;
            }

            public override object Execute(ScribeContext context)
            {
                var evt = ScribeEngine.Instance.GetEvent(EventName);
                if (evt == null)
                    throw new Exception($"Event '{EventName}' not found");

                // Capture the *current* context so that Trigger will inject into it
                var script = new ScribeScript { Root = Body };
                evt.RegisterHandler(script, context);
                return null;
            }
        }

        public class ScribeScript
        {
            public ScribeNode Root { get; set; }
        }
    }

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


    /// <summary>
    /// Parser for the Scribe language
    /// </summary>
    public class ScribeParser
    {
        private List<Token> _tokens;
        private int _position;
        private Token _current => _position < _tokens.Count ? _tokens[_position] : null;

        public BinaryOpNode.ScribeScript Parse(string source)
        {
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
            if (_current.Type != type)
                throw new Exception($"Expected {type}, got {_current.Type} at {_current.Line}:{_current.Column}");
            if (value != null && _current.Value != value)
                throw new Exception($"Expected '{value}', got '{_current.Value}' at {_current.Line}:{_current.Column}");
            var t = _current;
            _position++;
            return t;
        }
    }

    /// <summary>
    /// Executes a parsed script
    /// </summary>
    public class ScribeExecutor
    {
        public object ExecuteScript(BinaryOpNode.ScribeScript script, ScribeContext context)
        {
            return script.Root.Execute(context);
        }
    }


    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"Vector3({X}, {Y}, {Z})";
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator *(Vector3 a, double s)
        {
            return new Vector3(a.X * s, a.Y * s, a.Z * s);
        }

        public static Vector3 operator /(Vector3 a, double s)
        {
            return new Vector3(a.X / s, a.Y / s, a.Z / s);
        }
    }

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

    [AttributeUsage(AttributeTargets.Class)]
    public class ScribeExtensionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ScribeTypeAttribute : Attribute
    {
        public string Name { get; }

        public ScribeTypeAttribute(string name = null)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ScribeFunctionAttribute : Attribute
    {
        public string Name { get; }

        public ScribeFunctionAttribute(string name = null)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ScribeEventAttribute : Attribute
    {
    }
}