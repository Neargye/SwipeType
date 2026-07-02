// Copyright 2018 - 2026 Daniil Goncharov <neargye@gmail.com>.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwipeType
{
    /// <summary>
    /// Ranks words by strict ordered path matching.
    /// </summary>
    public class MatchSwipeType : SwipeType
    {
        private readonly Dictionary<int, WordEntry[]> candidatesByEndpoint;

        /// <summary>
        /// Keyboard layout.
        /// </summary>
        private static readonly string[] KeyboardLayoutEnglish =
        {
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm"
        };

        /// <summary>
        /// Initializes a strict path-matching swipe suggestion provider.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public MatchSwipeType(IEnumerable<string> wordList) : base(wordList)
        {
            candidatesByEndpoint = WordEntries
                .GroupBy(x => GetEndpointKey(x.NormalizedWord[0], x.NormalizedWord[x.NormalizedWord.Length - 1]))
                .ToDictionary(x => x.Key, x => x.ToArray());
        }

        /// <summary>
        /// Returns suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        protected override IEnumerable<string> GetSuggestionImpl(string input)
        {
            int minimumWordLength = GetMinimumWordLength(input);
            if (!candidatesByEndpoint.TryGetValue(GetEndpointKey(input[0], input[input.Length - 1]), out var candidates))
            {
                return Enumerable.Empty<string>();
            }

            return candidates
                .Where(x => x.NormalizedWord.Length > minimumWordLength)
                .Where(x => Match(input, x.NormalizedWord))
                .OrderBy(x => TextDistance.GetDamerauLevenshteinDistance(input, x.NormalizedWord))
                .ThenBy(x => x.Word, StringComparer.Ordinal)
                .Select(x => x.Word);
        }

        private static int GetEndpointKey(char first, char last)
        {
            return (first << 16) | last;
        }

        /// <summary>
        /// Checks whether all word letters appear in the swipe path in order.
        /// </summary>
        private static bool Match(string path, string word)
        {
            int i = 0;
            foreach (char c in path)
            {
                if (c == word[i])
                {
                    ++i;
                }

                if (i == word.Length)
                {
                    return true;
                }
            }

            return i == word.Length;
        }

        /// <summary>
        /// Returns the row number of the character.
        /// </summary>
        private static int GetKeyboardRow(char c)
        {
            for (int i = 0; i < KeyboardLayoutEnglish.Length; ++i)
            {
                if (KeyboardLayoutEnglish[i].Contains(c))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes redundant sequential characters.
        /// </summary>
        private static StringBuilder Compress(StringBuilder sequence)
        {
            // Example: 11123311 => 1231.

            if (sequence == null || sequence.Length == 0)
            {
                return new StringBuilder();
            }

            var s = new StringBuilder();
            s.Append(sequence[0]);

            for (int i = 1; i < sequence.Length; ++i)
            {
                if (s[s.Length - 1] != sequence[i])
                {
                    s.Append(sequence[i]);
                }
            }

            return s;
        }

        /// <summary>
        /// Returns the minimum possible word length from the path.
        /// Uses the number of transitions from different rows in the keyboard layout to determine the minimum length.
        /// </summary>
        private static int GetMinimumWordLength(string path)
        {
            var rowNumbers = new StringBuilder();
            foreach (char inChar in path)
            {
                int i = GetKeyboardRow(inChar);
                if (i >= 0)
                {
                    rowNumbers.Append(i);
                }
            }

            return Math.Max(0, Compress(rowNumbers).Length - 3);
        }
    }
}
