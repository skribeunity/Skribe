using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace UnitTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void Test1()
        {
            ClassicAssert.True(true);
        }
    }
}