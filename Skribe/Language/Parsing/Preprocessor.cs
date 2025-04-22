using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Skribe.Language.Parsing
{
    /// <summary>
    /// Flexible pre‑processor that converts natural‑language phrases into
    /// classic Scribe symbols and removes noise words.
    ///
    ///  ✦ Phrase rules (Regex → replacement) run first – good for multi‑word tokens.
    ///  ✦ Simple token rules (single word → replacement) run next.
    ///  ✦ Removable words are then stripped.
    ///
    /// All three layers are fully extensible at runtime.
    /// </summary>
    public class Preprocessor
    {
        /* --------------------------------------------------------------------
         *  1.  Data‑stores
         * ------------------------------------------------------------------*/
        private readonly List<(Regex pattern, string replacement)> _phraseRules = new();
        private readonly Dictionary<string, string> _tokenReplacements = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _removables          = new(StringComparer.OrdinalIgnoreCase);

        /* --------------------------------------------------------------------
         *  2.  Ctor – registers the default English ➜ symbol dictionary
         * ------------------------------------------------------------------*/
        public Preprocessor(bool registerDefaults = true)
        {
            if (!registerDefaults) return;

            // ----- comparison
            RegisterPhraseReplacement(@"\bis\s+greater\s+than\s+or\s+equal\s+to\b", ">=");
            RegisterPhraseReplacement(@"\bis\s+less\s+than\s+or\s+equal\s+to\b",    "<=");
            RegisterPhraseReplacement(@"\bis\s+greater\s+than\b",                   ">");
            RegisterPhraseReplacement(@"\bis\s+less\s+than\b",                      "<");
            RegisterPhraseReplacement(@"\bis\s+not\s+equal\s+to\b",                 "!=");
            RegisterPhraseReplacement(@"\bis\s+equal\s+to\b|\bequals\b",            "==");

            // ----- arithmetic
            RegisterPhraseReplacement(@"\bplus\b",                "+");
            RegisterPhraseReplacement(@"\bminus\b",               "-");
            RegisterPhraseReplacement(@"\btimes\b|\bmultiplied\s+by\b", "*");
            RegisterPhraseReplacement(@"\bdivided\s+by\b",        "/");
            RegisterPhraseReplacement(@"\bmodulo\b|\bmod\b",      "%");

            // ----- assignment sugar (“set x to 5” ➜ “var x = 5”)
            RegisterPhraseReplacement(@"\bset\s+([A-Za-z_]\w*)\s+to\b", "var $1 =");

            // ----- noise words you usually don’t want
            RegisterRemovableKeyword("then");
            RegisterRemovableKeyword("end");
            RegisterPhraseReplacement(@"\bend\s+(?:if|while|for|function)\b", "");
        }

        /* --------------------------------------------------------------------
         *  3.  Public registration API
         * ------------------------------------------------------------------*/
        public void RegisterPhraseReplacement(string pattern, string replacement,
                                              RegexOptions options = RegexOptions.IgnoreCase)
            => _phraseRules.Add((new Regex(pattern, options | RegexOptions.Compiled), replacement));

        public void RegisterTokenReplacement(string word, string replacement)
            => _tokenReplacements[word] = replacement;

        public void RegisterRemovableKeyword(string keyword)
            => _removables.Add(keyword);

        /* --------------------------------------------------------------------
         *  4.  Processing entry‑point
         * ------------------------------------------------------------------*/
        public string Process(string input)
        {
            var stringRegex = new Regex(@"(""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*')",
                RegexOptions.Compiled);

            // Split on *string literals* first so we never touch them
            var segments = stringRegex.Split(input);

            for (int i = 0; i < segments.Length; i++)
            {
                if (i % 2 != 0) continue;                       // inside quotes: leave intact

                // Next, split the non‑string chunk by lines
                var lines = segments[i].Split('\n');
                for (int ln = 0; ln < lines.Length; ln++)
                {
                    var line = lines[ln];
                    var trimmed = line.TrimStart();

                    //  ── leave comment lines exactly as they are ──
                    if (trimmed.StartsWith("#"))
                        continue;

                    lines[ln] = ProcessNonStringLine(line);
                }
                segments[i] = string.Join("\n", lines);
            }
            return string.Concat(segments);
        }

        /* ----------------------------------------------------- */
        private string ProcessNonStringLine(string line)
        {
            /* 5‑A  phrase rules */
            foreach (var (re, repl) in _phraseRules)
                line = re.Replace(line, repl);

            /* 5‑B  token rules & removables  (unchanged — just lifted from old code) */
            var tokenRegex = new Regex(@"(\s+|\w+|\S)", RegexOptions.Compiled);
            var tokens = tokenRegex.Matches(line).Cast<Match>().Select(m => m.Value).ToList();

            var processed = new List<string>();
            foreach (var tok in tokens)
            {
                if (_removables.Contains(tok)) continue;

                processed.Add(_tokenReplacements.TryGetValue(tok, out var repl) ? repl : tok);
            }
            return MergeWhitespace(processed);
        }

        private static string MergeWhitespace(IReadOnlyList<string> tokens)
        {
            var merged = new List<string>(tokens.Count);
            bool lastWasSpace = false;

            foreach (var t in tokens)
            {
                bool isSpace = t.All(char.IsWhiteSpace);
                if (isSpace)
                {
                    if (!lastWasSpace)
                        merged.Add(" ");
                    lastWasSpace = true;
                }
                else
                {
                    merged.Add(t);
                    lastWasSpace = false;
                }
            }

            return string.Concat(merged);
        }
    }
}
