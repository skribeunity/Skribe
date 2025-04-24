using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Skribe;
using Skribe.Language;
using Skribe.Language.Memory;

namespace UnitTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void LoaderTests()
        {
            Skribe.Skribe.Stop();
            Skribe.Skribe.Start();
            TestContext.WriteLine(Path.GetFullPath(Skribe.Skribe.Configuration.SkribePath));
        }

        [Test]
        public void SkribeLoaderTest()
        {
            var engine = SkribeEngine.Instance;
            engine.RegisterFunction(new ScribeFunction("log", args =>
            {
                var content = args[0];
                Console.WriteLine(content);
                return null;
            }, new[]
            {
                new ScribeParameter("content", "text"),
            }));
            Skribe.Skribe.Stop();
            var configuration = new SkribeConfiguration();
            var dir = Directory.GetParent("./").Parent.Parent.Parent;
            configuration.SkribePath = $"{dir.FullName}/testSkribes";
            Skribe.Skribe.Start(configuration);
            TestContext.WriteLine(Path.GetFullPath(Skribe.Skribe.Configuration.SkribePath));
        }
    }
}