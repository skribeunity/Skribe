using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Skribe.Language.Parsing
{
    public class Preprocessor
    {
        private readonly Dictionary<string, string> _replacements = new();
        private readonly HashSet<string> _removables = new();

        public void RegisterReplacement(string alternative, string replacement)
        {
            _replacements[alternative] = replacement;
        }

        public void RegisterRemovable(string keyword)
        {
            _removables.Add(keyword);
        }

        public string Process(string input)
        {
            var stringRegex = new Regex(@"(""(?:\\""|.)*?""|'(?:\\'|.)*?')");
            var segments = stringRegex.Split(input);

            for (var i = 0; i < segments.Length; i++)
            {
                if (i % 2 == 0)
                {
                    segments[i] = ProcessNonStringSegment(segments[i]);
                }
            }

            return string.Concat(segments);
        }

        private string ProcessNonStringSegment(string segment)
        {
            var tokenRegex = new Regex(@"(\s+|\w+|\S)");
            var tokens = tokenRegex.Matches(segment)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();

            var processedTokens = new List<string>();
            foreach (var token in tokens)
            {
                if (_removables.Contains(token))
                    continue;

                processedTokens.Add(_replacements.TryGetValue(token, out var replacement) 
                    ? replacement 
                    : token);
            }

            return MergeWhitespace(processedTokens);
        }

        private string MergeWhitespace(List<string> tokens)
        {
            var merged = new List<string>();
            bool previousWhitespace = false;
            
            foreach (var token in tokens)
            {
                bool isWhitespace = token.All(c => char.IsWhiteSpace(c));
                
                if (isWhitespace)
                {
                    if (!previousWhitespace)
                    {
                        merged.Add(" ");
                        previousWhitespace = true;
                    }
                }
                else
                {
                    merged.Add(token);
                    previousWhitespace = false;
                }
            }
            
            return string.Concat(merged);
        }
    }
}