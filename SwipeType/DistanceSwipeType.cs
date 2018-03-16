using System;
using System.Collections.Generic;

namespace SwipeType
{
    /// <summary>
    /// SwipeType using Damerau–Levenshtein distance.
    /// </summary>
    public class DistanceSwipeType : SwipeType
    {
        /// <summary>
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public DistanceSwipeType(string[] wordList) : base(wordList) { }

        /// <summary>
        ///     Returns suggestions for a given inputStr.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override string[] GetSuggestion(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var inputStr = input.ToLower();
            var listSuggestion = new List<string>();

            int min = int.MaxValue;
            foreach (var s in Words)
            {
                int d = TextDistance.GetDamerauLevenshteinDistance(inputStr, s);
                int t = Math.Min(min, d);
                if (min > t)
                    listSuggestion.Clear();

                min = t;

                if (d == min)
                    listSuggestion.Add(s);
            }

            return listSuggestion.ToArray();
        }
    }
}