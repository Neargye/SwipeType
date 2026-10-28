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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SwipeType
{
    /// <summary>
    /// Suggests only words whose letters all appear in the swipe path in order, with exactly the same first
    /// and last letters as the path. A doubled letter needs to appear in the path only once.
    /// </summary>
    /// <remarks>
    /// Words that pass the filter are ranked by Damerau-Levenshtein distance to the path, then by frequency,
    /// then by ordinal spelling. The layout rows only bound the shortest possible word; path characters are
    /// compared exactly.
    /// </remarks>
    public sealed class StrictSwipeSuggester : SwipeSuggester
    {
        private readonly Dictionary<int, WordEntry[]> candidatesByEndpoint;
        private readonly KeyboardLayout layout;

        /// <summary>
        /// Initializes a strict suggester for the English QWERTY layout.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <exception cref="ArgumentNullException"><paramref name="words"/> is null.</exception>
        public StrictSwipeSuggester(IEnumerable<string> words)
            : this(words, KeyboardLayout.Qwerty, NoFrequencies) { }

        /// <summary>
        /// Initializes a strict suggester for the English QWERTY layout
        /// with word frequencies.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="wordFrequencies">
        /// Word frequencies, matched case-insensitively, that order words with equal edit distance.
        /// Values must be finite and non-negative; entries for words outside <paramref name="words"/> are ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="words"/> or <paramref name="wordFrequencies"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency is negative, NaN, or infinite.</exception>
        public StrictSwipeSuggester(IEnumerable<string> words, IReadOnlyDictionary<string, double> wordFrequencies)
            : this(words, KeyboardLayout.Qwerty, wordFrequencies) { }

        /// <summary>
        /// Initializes a strict suggester for a custom keyboard layout.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="layout">Layout whose rows (<see cref="KeyboardKey.Y"/>) bound the shortest possible word.</param>
        /// <exception cref="ArgumentNullException"><paramref name="words"/> or <paramref name="layout"/> is null.</exception>
        public StrictSwipeSuggester(IEnumerable<string> words, KeyboardLayout layout)
            : this(words, layout, NoFrequencies) { }

        /// <summary>
        /// Initializes a strict suggester for a custom keyboard layout
        /// with word frequencies.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="layout">Layout whose rows (<see cref="KeyboardKey.Y"/>) bound the shortest possible word.</param>
        /// <param name="wordFrequencies">
        /// Word frequencies, matched case-insensitively, that order words with equal edit distance.
        /// Values must be finite and non-negative; entries for words outside <paramref name="words"/> are ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency is negative, NaN, or infinite.</exception>
        public StrictSwipeSuggester(IEnumerable<string> words, KeyboardLayout layout,
            IReadOnlyDictionary<string, double> wordFrequencies) : base(words, wordFrequencies)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            candidatesByEndpoint = WordEntries
                .GroupBy(x => GetEndpointKey(x.NormalizedWord[0], x.NormalizedWord[x.NormalizedWord.Length - 1]))
                .ToDictionary(x => x.Key, x => x.ToArray());
        }

        private protected override IReadOnlyList<string> GetSuggestionsCore(string path, int maxCount)
        {
            if (!candidatesByEndpoint.TryGetValue(GetEndpointKey(path[0], path[path.Length - 1]), out var candidates))
            {
                return Array.Empty<string>();
            }

            int minimumWordLength = GetMinimumWordLength(path);
            var workspace = new TextDistance.Workspace();
            var top = CreateTopCandidateBuffer(maxCount, EditDistanceCandidate.Comparer);
            foreach (WordEntry wordEntry in candidates)
            {
                if (wordEntry.NormalizedWord.Length >= minimumWordLength && Match(path, wordEntry.NormalizedWord))
                {
                    top.Add(new EditDistanceCandidate(
                        wordEntry.Word, workspace.GetDistance(path, wordEntry.NormalizedWord), wordEntry.Frequency));
                }
            }

            return top.FinalizeSortedCandidates().Select(x => x.Word).ToArray();
        }

        private static int GetEndpointKey(char first, char last)
        {
            return (first << 16) | last;
        }

        /// <summary>
        /// Checks whether all word letters appear in the swipe path in order. A swipe passes a doubled letter
        /// once, so a run of equal letters needs only one path character.
        /// </summary>
        private static bool Match(string path, string word)
        {
            int i = 0;
            foreach (char c in path)
            {
                while (i < word.Length && c == word[i])
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
        /// Returns the minimum possible word length from the path.
        /// Uses the number of transitions between different keyboard rows to determine the minimum length.
        /// </summary>
        private int GetMinimumWordLength(string path)
        {
            double previousRow = 0.0;
            bool hasPreviousRow = false;
            int compressedRowLength = 0;
            foreach (char c in path)
            {
                int keyIndex = layout.GetKeyIndex(c);
                if (keyIndex < 0)
                {
                    continue;
                }

                double row = layout.GetRow(keyIndex);
                if (!hasPreviousRow || row != previousRow)
                {
                    ++compressedRowLength;
                    previousRow = row;
                    hasPreviousRow = true;
                }
            }

            return Math.Max(0, compressedRowLength - 3);
        }
    }
}
