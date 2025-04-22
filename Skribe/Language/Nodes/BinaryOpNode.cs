using System;
using System.Collections.Generic;
using System.Linq;
using Skribe.Language.Memory;
using Skribe.Language.Misc;

namespace Skribe.Language.Nodes
{
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
}