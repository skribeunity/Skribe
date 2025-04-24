using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Skribe.Language;
using Skribe.Language.Memory;
using Skribe.Language.Misc;
using Skribe.Language.Parsing;
using Skribe.Language.Types;

namespace SkribeUnitTests
{
    /// <summary>
    /// Unit tests for the Scribe language
    /// </summary>
    [TestFixture]
    public class SkribeTests
    {
        private SkribeEngine _engine;

        [SetUp]
        public void Setup()
        {
            // Reset the singleton
            typeof(SkribeEngine)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            _engine = SkribeEngine.Instance;
            Skribe.Skribe.Start();
        }

        [Test]
        public void TestLiteralExpressions()
        {
            ClassicAssert.AreEqual(42.0f, _engine.Execute("42"));
            ClassicAssert.AreEqual("hello", _engine.Execute("\"hello\""));
            ClassicAssert.AreEqual(true, _engine.Execute("true"));
            ClassicAssert.AreEqual(false, _engine.Execute("false"));
            ClassicAssert.IsNull(_engine.Execute("null"));
        }

        [Test]
        public void TestArithmeticOperations()
        {
            ClassicAssert.AreEqual(3.0, _engine.Execute("1 + 2"));
            ClassicAssert.AreEqual(-1.0, _engine.Execute("1 - 2"));
            ClassicAssert.AreEqual(6.0, _engine.Execute("2 * 3"));
            ClassicAssert.AreEqual(2.0, _engine.Execute("6 / 3"));
            ClassicAssert.AreEqual(1.0, _engine.Execute("4 % 3"));

            // Precedence
            ClassicAssert.AreEqual(14.0, _engine.Execute("2 + 3 * 4"));
            ClassicAssert.AreEqual(20.0, _engine.Execute("(2 + 3) * 4"));

            // Unary
            ClassicAssert.AreEqual(-5.0, _engine.Execute("-5"));
        }

        [Test]
        public void TestStringOperations()
        {
            ClassicAssert.AreEqual("hello world", _engine.Execute("\"hello\" + \" world\""));
            ClassicAssert.AreEqual("hello42", _engine.Execute("\"hello\" + 42"));
        }

        [Test]
        public void TestComparisonOperations()
        {
            ClassicAssert.AreEqual(true, _engine.Execute("1 < 2"));
            ClassicAssert.AreEqual(false, _engine.Execute("1 > 2"));
            ClassicAssert.AreEqual(true, _engine.Execute("1 <= 1"));
            ClassicAssert.AreEqual(true, _engine.Execute("1 >= 1"));
            ClassicAssert.AreEqual(true, _engine.Execute("1 == 1"));
            ClassicAssert.AreEqual(false, _engine.Execute("1 != 1"));

            // Strings
            ClassicAssert.AreEqual(true, _engine.Execute("\"a\" < \"b\""));
            ClassicAssert.AreEqual(true, _engine.Execute("\"a\" == \"a\""));
        }

        [Test]
        public void TestLogicalOperations()
        {
            ClassicAssert.AreEqual(true, _engine.Execute("true and true"));
            ClassicAssert.AreEqual(false, _engine.Execute("true and false"));
            ClassicAssert.AreEqual(true, _engine.Execute("true or false"));
            ClassicAssert.AreEqual(false, _engine.Execute("false or false"));
            ClassicAssert.AreEqual(false, _engine.Execute("not true"));
            ClassicAssert.AreEqual(true, _engine.Execute("not false"));

            // short‑circuit; "nonexistent" is never evaluated
            ClassicAssert.AreEqual(false, _engine.Execute("false and nonexistent"));
        }

        [Test]
        public void TestVariables()
        {
            var ctx = new ScribeContext();

            _engine.Execute("var x = 42", ctx);
            ClassicAssert.AreEqual(42.0, _engine.Execute("x", ctx));

            _engine.Execute("x = 10", ctx);
            ClassicAssert.AreEqual(10.0, _engine.Execute("x", ctx));

            // child scope
            var child = ctx.CreateChildContext();
            _engine.Execute("var y = 20", child);
            ClassicAssert.AreEqual(20.0, _engine.Execute("y", child));
            ClassicAssert.AreEqual(10.0, _engine.Execute("x", child));

            // undefined
            ClassicAssert.Throws<Exception>(() => _engine.Execute("z", ctx));
            ClassicAssert.Throws<Exception>(() => _engine.Execute("y", ctx));
        }

        [Test]
        public void TestIfStatement()
        {
            var ctx = new ScribeContext();

            _engine.Execute(@"
                var x = 10
                if (x > 5) {
                  x = 20
                } else {
                  x = 0
                }
            ", ctx);
            ClassicAssert.AreEqual(20.0, _engine.Execute("x", ctx));

            _engine.Execute(@"
                x = 3
                if (x > 5) {
                  x = 20
                } else {
                  x = 0
                }
            ", ctx);
            ClassicAssert.AreEqual(0.0, _engine.Execute("x", ctx));
        }

        [Test]
        public void TestWhileLoop()
        {
            var ctx = new ScribeContext();

            _engine.Execute(@"
                var x = 0
                var i = 0
                while (i < 5) {
                  x = x + i
                  i = i + 1
                }
            ", ctx);
            ClassicAssert.AreEqual(10.0, _engine.Execute("x", ctx));
        }

        [Test]
        public void TestForLoop()
        {
            var ctx = new ScribeContext();

            _engine.Execute(@"
                var sum = 0
                for (var i = 0; i < 5; i = i + 1) {
                  sum = sum + i
                }
            ", ctx);
            ClassicAssert.AreEqual(10.0, _engine.Execute("sum", ctx));
        }

        [Test]
        public void TestFunctions()
        {
            var ctx = new ScribeContext();

            _engine.Execute(@"
                function add(a, b) { return a + b }
                var result = add(2, 3)
            ", ctx);
            ClassicAssert.AreEqual(5.0, _engine.Execute("result", ctx));

            // recursion
            _engine.Execute(@"
                function factorial(n) {
                  if (n <= 1) return 1
                  return n * factorial(n - 1)
                }
                var fact5 = factorial(5)
            ", ctx);
            ClassicAssert.AreEqual(120.0, _engine.Execute("fact5", ctx));
        }

        [Test]
        public void TestEvents()
        {
            // register a new event
            _engine.RegisterEvent(new ScribeEvent("testEvent", new[]
            {
                new ScribeParameter("value", "number")
            }));

            // capture Console output
            var sw = new StringWriter();
            var original = Console.Out;
            Console.SetOut(sw);
            try
            {
                _engine.Execute(@"
                    on testEvent {
                        print(""Event value: "" + value)
                    }
                ");
                _engine.TriggerEvent("testEvent", 42.0);
            }
            finally
            {
                Console.SetOut(original);
            }

            var outText = sw.ToString();
            ClassicAssert.IsTrue(outText.Contains("Event value: 42"));
        }

        [Test]
        public void TestGameIntegration()
        {
            // now top‑level
            ScribeGameIntegration.RegisterGameTypes(_engine);
            ScribeGameIntegration.RegisterGameFunctions(_engine);
            ScribeGameIntegration.RegisterGameEvents(_engine);

            var ctx = new ScribeContext();
            var player = new Player
            {
                Name = "TestPlayer",
                Health = 100,
                Position = new Vector3(0, 0, 0)
            };
            ctx.SetVariable("player", player);

            // property access
            ClassicAssert.AreEqual("TestPlayer", _engine.Execute("player.Name", ctx));
            ClassicAssert.AreEqual(100.0, _engine.Execute("player.Health", ctx));

            // call a game function
            _engine.Execute(@" spawn(""enemy"", player.Position) ", ctx);

            // handle a game event
            _engine.Execute(@"
                var damageHandled = false
                on playerDamage {
                  damageHandled = true
                  player.Health = player.Health - amount
                }
            ", ctx);

            _engine.TriggerEvent("playerDamage", player, 25.0, "fire");

            ClassicAssert.AreEqual(true, _engine.Execute("damageHandled", ctx));
            ClassicAssert.AreEqual(75.0, _engine.Execute("player.Health", ctx));
        }

        [Test]
        public void TestExtensionSystem()
        {
            // top‐level
            SkribeExtensionSystem.LoadExtensionsFromAssembly(typeof(SkribeTests).Assembly);

            var ctx = new ScribeContext();
            _engine.Execute(@" var enemy = createEnemy(""Goblin"", 50) ", ctx);

            var enemyObj = ctx.GetVariable("enemy").Value;
            ClassicAssert.IsInstanceOf<Enemy>(enemyObj);
            var goblin = (Enemy)enemyObj;
            ClassicAssert.AreEqual("Goblin", goblin.Name);
            ClassicAssert.AreEqual(50.0, goblin.Health);

            // C# Attack()
            var hero = new Player { Name = "Hero", Health = 100, Position = new Vector3(0, 0, 0) };
            ctx.SetVariable("player", hero);
            _engine.Execute(@" Attack(enemy, player, 20) ", ctx);
            ClassicAssert.AreEqual(80.0, hero.Health);
        }
    }
}


public class ScribeGameIntegration
{
    public static void RegisterGameTypes(SkribeEngine engine)
    {
        engine.RegisterType(new ScribeType("Player", typeof(Player)));
        engine.RegisterType(new ScribeType("Vector", typeof(Vector3)));
    }

    public static void RegisterGameFunctions(SkribeEngine engine)
    {
        engine.RegisterFunction(new ScribeFunction("spawn", args =>
        {
            var et = args[0]?.ToString();
            var pos = (Vector3)args[1];
            Console.WriteLine($"Spawned {et} at {pos}");
            return null;
        }, new[]
        {
            new ScribeParameter("entityType", "text"),
            new ScribeParameter("position", "Vector")
        }));
    }

    public static void RegisterGameEvents(SkribeEngine engine)
    {
        engine.RegisterEvent(new ScribeEvent("playerJoin", new[]
        {
            new ScribeParameter("player", "Player")
        }));
        engine.RegisterEvent(new ScribeEvent("playerDamage", new[]
        {
            new ScribeParameter("player", "Player"),
            new ScribeParameter("amount", "number"),
            new ScribeParameter("damageType", "text")
        }));
    }
}

public class Player
{
    public string Name { get; set; }
    public double Health { get; set; }
    public Vector3 Position { get; set; }

    public override string ToString()
    {
        return $"Player({Name}, Health={Health})";
    }
}


[ScribeExtension]
[ScribeType("Enemy")]
public class Enemy
{
    public string Name { get; set; }
    public double Health { get; set; }
    public Vector3 Position { get; set; }

    [ScribeFunction("createEnemy")]
    public static Enemy Create(string name, double health, Vector3 position = null)
    {
        return new Enemy { Name = name, Health = health, Position = position ?? new Vector3(0, 0, 0) };
    }

    [ScribeFunction]
    public static void Attack(Enemy enemy, Player player, double damage)
    {
        Console.WriteLine($"{enemy.Name} attacks {player.Name} for {damage} damage!");
        player.Health -= damage;
    }

    [ScribeEvent] public static ScribeEvent EnemySpawn = new ScribeEvent("enemySpawn", new[]
    {
        new ScribeParameter("enemy", "Enemy"),
        new ScribeParameter("location", "Vector3")
    });
}