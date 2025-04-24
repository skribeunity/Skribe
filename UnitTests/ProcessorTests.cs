using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Skribe.Language.Parsing;

namespace UnitTests
{
    [TestFixture]
    public class ProcessorTests
    {
        /// <summary>
        /// Collapses every run of whitespace to a single space and trims ends,
        /// so tests are agnostic to line‑break style or indentation.
        /// </summary>
        private static string Normalize(string s) =>
            Regex.Replace(s, @"\s+", " ").Trim();

        /*──────────────────────────────────────────────────────────────────*/
        /*  1.  Arithmetic & assignment                                   */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void SetPlus_RewritesToVarAndPlus()
        {
            var src  = "set total to price plus tax";
            var dest = new Preprocessor().Process(src);
            var expect = "var total = price + tax";
            ClassicAssert.AreEqual(Normalize(expect), Normalize(dest));
        }

        [Test]
        public void MinusTimesDivided_Modulo_AllRewrite()
        {
            var src = @"
                set a to x minus y
                set b to x times y
                set c to x divided by y
                set d to x modulo y";
            var dest = new Preprocessor().Process(src);
            var expect = @"
                var a = x - y
                var b = x * y
                var c = x / y
                var d = x % y";
            ClassicAssert.AreEqual(Normalize(expect), Normalize(dest));
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  2.  Comparison operators                                      */
        /*──────────────────────────────────────────────────────────────────*/

        [TestCase("is greater than",          ">")]
        [TestCase("is less than",             "<")]
        [TestCase("is greater than or equal to", ">=")]
        [TestCase("is less than or equal to",    "<=")]
        [TestCase("is equal to",              "==")]
        [TestCase("equals",                   "==")]
        [TestCase("is not equal to",          "!=")]
        public void ComparisonPhrases_RewriteCorrectly(string phrase, string symbol)
        {
            var src  = $"if score {phrase} 50 then pass = true end if";
            var dest = new Preprocessor().Process(src);
            StringAssert.Contains($"score {symbol} 50", dest);
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  3.  Keyword removal                                           */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void NoiseWords_Removed()
        {
            var src  = "if x > 0 then y = 1 end if";
            var dest = new Preprocessor().Process(src);

            ClassicAssert.False(dest.Contains(" then "));
            ClassicAssert.False(dest.Contains(" end "));
            StringAssert.DoesNotEndWith(" if", dest.Trim());
        }


        /*──────────────────────────────────────────────────────────────────*/
        /*  4.  Strings & comments stay untouched                          */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void StringLiteral_IsNotChanged()
        {
            var src  = "print(\"a plus b is\", a plus b)";
            var dest = new Preprocessor().Process(src);
            StringAssert.Contains("\"a plus b is\"", dest);
        }

        [Test]
        public void CommentLine_Remains()
        {
            var src  = "# compute modulo\nset n to x modulo 2";
            var dest = new Preprocessor().Process(src);

            StringAssert.StartsWith("# compute modulo", dest.TrimStart());
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  5.  Whitespace collapse                                        */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void MultipleSpacesAndNewlines_CollapseToSingleSpaces()
        {
            var src = "set     x  to   1\n\nset   y  to  2";
            var dest = new Preprocessor().Process(src);
            var expected = "var x = 1 var y = 2";
            ClassicAssert.AreEqual(expected, Normalize(dest));
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  6.  Custom rule registration                                   */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void CustomPhraseRule_Works()
        {
            var pp = new Preprocessor();                        // defaults ON
            pp.RegisterPhraseReplacement(@"\btwice\s+as\s+big\b", "* 2");

            var dest = pp.Process("set width to base twice as big");
            ClassicAssert.AreEqual("var width = base * 2", Normalize(dest));
        }

        [Test]
        public void CustomTokenRule_Works()
        {
            var pp = new Preprocessor(registerDefaults: false);
            pp.RegisterTokenReplacement("plus", "+");
            var dest = pp.Process("a plus b");
            ClassicAssert.AreEqual("a + b", Normalize(dest));
        }

        [Test]
        public void CustomRemovable_Works()
        {
            var pp = new Preprocessor(registerDefaults: true);
            pp.RegisterRemovableKeyword("please");
            var dest = pp.Process("please set x to 1 please");
            ClassicAssert.AreEqual("var x = 1", Normalize(dest));
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  7.  Case‑insensitivity                                         */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void PhraseMatching_IsCaseInsensitive()
        {
            var src  = "IF total IS GREATER THAN 10 THEN y = 1 END IF";
            var dest = new Preprocessor().Process(src);
            StringAssert.Contains("total > 10", dest);
        }

        /*──────────────────────────────────────────────────────────────────*/
        /*  8.  Complex mixed example                                      */
        /*──────────────────────────────────────────────────────────────────*/

        [Test]
        public void MixedExample_ProducesExpectedOutput()
        {
            var src = @"
        set n to 0
        while n is less than 3 then
            print(""n ="", n)
            set n to n plus 1
        end while";

            var dest = new Preprocessor().Process(src);

            var expect = @"
        var n = 0
        while n < 3
            print(""n ="", n)
            var n = n + 1";

            ClassicAssert.AreEqual(Normalize(expect), Normalize(dest));
        }
    }
}
