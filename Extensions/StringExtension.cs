using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Extensions
{
    public static class StringExtension
    {
        public static string GetWordWithFirstLetterDown(this string word)
        {
            return $"{word.ToLower()[0]}{word.Substring(1)}";
        }

        public static string GetWordWithFirstLetterUpper(this string word)
        {
            return $"{word.ToUpper()[0]}{word.Substring(1)}";
        }

        public static string ReplaceFirstOccurrence(this string texto, string oldValue, string newValue)
        {
            int index = texto.IndexOf(oldValue);
            if (index < 0)
            {
                return texto;
            }
            return texto.Substring(0, index) + newValue + texto.Substring(index + oldValue.Length);
        }

        public static string[] SubstringsBetween(this string str, string initialString, string finalString)
        {
            var substrings = new List<string>();

            string escapedInitial = Regex.Escape(initialString);
            string escapedFinal = Regex.Escape(finalString);

            string pattern = $@"(?<={escapedInitial})(.*?)(?={escapedFinal})";

            MatchCollection matches = Regex.Matches(str, pattern);

            foreach (Match match in matches)
            {
                substrings.Add(match.Value);
            }

            return [.. substrings];
        }

        public static string ToPlural(this string word)
        {
            var pluralization = new EnglishPluralizationService();

            return pluralization.Pluralize(word);
        }
    }
}