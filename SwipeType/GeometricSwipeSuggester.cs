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
    /// Ranks words by keyboard geometry, tolerating nearby keys and path noise. The recommended suggester for
    /// swipe input.
    /// </summary>
    /// <remarks>
    /// Path characters that are not on the layout are ignored. Words whose first or last letter is not on
    /// the layout are never suggested. Candidates are ordered by match class (exact ordered match, nearby
    /// ordered match, other), then by geometric score including the optional frequency boost, then by
    /// Damerau-Levenshtein distance to the path, then by ordinal spelling. In an exact ordered match the word
    /// starts and ends with the path's first and last characters, and its letters (a doubled letter once) lie
    /// in order on path keys at distance zero, so keys placed at the same position count as one key.
    /// </remarks>
    public sealed class GeometricSwipeSuggester : SwipeSuggester
    {
        private const double MissingLetterCost = 1.35;
        private const double RepeatedLetterCost = 0.3;
        private const double ExtraPathCharacterCost = 0.03;
        private const double EndpointWeight = 1.5;
        private const double LengthDifferenceWeight = 0.08;
        private const double UnsupportedCharacterCost = 2.0;
        private const double EndpointCandidateRadius = 1.25;
        private const double OrderedMatchRadius = 1.25;
        private const double ExactOrderedMatchBoost = 2.0;
        private const double NearbyOrderedMatchBoost = 1.0;
        private const double OrderedMatchLetterBoost = 0.1;
        private const double FrequencyWeight = 8.0;

        private readonly Dictionary<int, WordEntry[]> candidatesByEndpoint;
        private readonly KeyboardLayout layout;
        private readonly char[][]? nearbyKeys;

        /// <summary>
        /// Initializes a geometric suggester for the English QWERTY layout.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <exception cref="ArgumentNullException"><paramref name="words"/> is null.</exception>
        public GeometricSwipeSuggester(IEnumerable<string> words)
            : this(words, KeyboardLayout.Qwerty, NoFrequencies) { }

        /// <summary>
        /// Initializes a geometric suggester for the English QWERTY layout with word frequencies.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="wordFrequencies">
        /// Word frequencies, matched case-insensitively. Values must be finite and non-negative; entries for
        /// words outside <paramref name="words"/> are ignored. A logarithmic boost is added to the geometric
        /// score, so frequencies reorder words within one match class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="words"/> or <paramref name="wordFrequencies"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency is negative, NaN, or infinite.</exception>
        public GeometricSwipeSuggester(IEnumerable<string> words, IReadOnlyDictionary<string, double> wordFrequencies)
            : this(words, KeyboardLayout.Qwerty, wordFrequencies) { }

        /// <summary>
        /// Initializes a geometric suggester for a custom keyboard layout.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="layout">Keyboard layout used for geometric scoring.</param>
        /// <exception cref="ArgumentNullException"><paramref name="words"/> or <paramref name="layout"/> is null.</exception>
        public GeometricSwipeSuggester(IEnumerable<string> words, KeyboardLayout layout)
            : this(words, layout, NoFrequencies) { }

        /// <summary>
        /// Initializes a geometric suggester for a custom keyboard layout with word frequencies.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="layout">Keyboard layout used for geometric scoring.</param>
        /// <param name="wordFrequencies">
        /// Word frequencies, matched case-insensitively. Values must be finite and non-negative; entries for
        /// words outside <paramref name="words"/> are ignored. A logarithmic boost is added to the geometric
        /// score, so frequencies reorder words within one match class.
        /// </param>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency is negative, NaN, or infinite.</exception>
        public GeometricSwipeSuggester(IEnumerable<string> words, KeyboardLayout layout,
            IReadOnlyDictionary<string, double> wordFrequencies) : base(words, wordFrequencies)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            nearbyKeys = layout.Distances == null ? null : CreateNearbyKeys(layout);
            candidatesByEndpoint = WordEntries
                .GroupBy(x => GetEndpointKey(x.NormalizedWord[0], x.NormalizedWord[x.NormalizedWord.Length - 1]))
                .ToDictionary(x => x.Key, x => x.ToArray());
        }

        private protected override IReadOnlyList<string> GetSuggestionsCore(string path, int maxCount)
        {
            string supportedPath = GetSupportedPath(path);
            if (supportedPath.Length == 0)
            {
                return Array.Empty<string>();
            }

            List<ScoredWord> top = GetTopSuggestions(supportedPath, GetPathKeyIndexes(supportedPath), maxCount);
            var suggestions = new string[top.Count];
            for (int i = 0; i < suggestions.Length; ++i)
            {
                suggestions[i] = top[i].Word;
            }

            return suggestions;
        }

        private List<ScoredWord> GetTopSuggestions(string path, int[] pathKeyIndexes, int count)
        {
            WordEntry[] candidates = GetCandidates(path);
            if (candidates.Length == 0)
            {
                return new List<ScoredWord>();
            }

            count = Math.Min(count, candidates.Length);
            var candidateRanks = new byte[candidates.Length];
            var candidateCountsByRank = new int[3];
            for (int i = 0; i < candidates.Length; ++i)
            {
                int orderedMatchRank = GetOrderedMatchRank(path, pathKeyIndexes, candidates[i].NormalizedWord);
                candidateRanks[i] = (byte)orderedMatchRank;
                ++candidateCountsByRank[orderedMatchRank];
            }

            // Match classes are the primary sort key, so each class is selected separately, best class first,
            // and only the classes needed to fill the requested count are scored.
            var previousAlignmentRow = new double[path.Length + 1];
            var currentAlignmentRow = new double[path.Length + 1];
            var comparer = new ScoredWordComparer(path);
            var top = new List<ScoredWord>(count);

            for (int rank = 0; rank < candidateCountsByRank.Length && top.Count < count; ++rank)
            {
                int rankCount = Math.Min(count - top.Count, candidateCountsByRank[rank]);
                if (rankCount == 0)
                {
                    continue;
                }

                var rankTop = CreateTopCandidateBuffer(rankCount, comparer);
                for (int i = 0; i < candidates.Length; ++i)
                {
                    if (candidateRanks[i] != rank)
                    {
                        continue;
                    }

                    var scoredWord = ScoreWord(path, pathKeyIndexes, candidates[i], rank,
                        previousAlignmentRow, currentAlignmentRow);
                    rankTop.Add(scoredWord);
                }

                top.AddRange(rankTop.FinalizeSortedCandidates());
            }

            return top;
        }

        /// <summary>
        /// Returns words with nearby endpoints whose length fits the path, or all words with nearby endpoints
        /// when none fits. Counts first so the result is allocated once at its exact size.
        /// </summary>
        private WordEntry[] GetCandidates(string path)
        {
            List<WordEntry[]> endpointBuckets = GetEndpointBuckets(path);
            int minimumLength = GetMinimumCandidateLength(path);
            int maximumLength = path.Length + 2;
            int endpointCount = 0;
            int lengthCount = 0;
            foreach (WordEntry[] bucket in endpointBuckets)
            {
                endpointCount += bucket.Length;
                foreach (WordEntry wordEntry in bucket)
                {
                    if (FitsLength(wordEntry, minimumLength, maximumLength))
                    {
                        ++lengthCount;
                    }
                }
            }

            if (lengthCount == 0)
            {
                minimumLength = 0;
                maximumLength = int.MaxValue;
            }

            var candidates = new WordEntry[lengthCount == 0 ? endpointCount : lengthCount];
            int index = 0;
            foreach (WordEntry[] bucket in endpointBuckets)
            {
                foreach (WordEntry wordEntry in bucket)
                {
                    if (FitsLength(wordEntry, minimumLength, maximumLength))
                    {
                        candidates[index++] = wordEntry;
                    }
                }
            }

            return candidates;
        }

        private static bool FitsLength(WordEntry wordEntry, int minimumLength, int maximumLength)
        {
            // A swipe passes a run of equal letters once, so the upper bound applies to the collapsed length.
            return wordEntry.NormalizedWord.Length >= minimumLength && wordEntry.CollapsedLength <= maximumLength;
        }

        private List<WordEntry[]> GetEndpointBuckets(string path)
        {
            char pathFirst = path[0];
            char pathLast = path[path.Length - 1];
            var buckets = new List<WordEntry[]>();
            if (nearbyKeys == null)
            {
                foreach (var bucket in candidatesByEndpoint)
                {
                    char candidateFirst = (char)(bucket.Key >> 16);
                    char candidateLast = (char)bucket.Key;
                    if (GetKeyDistance(pathFirst, candidateFirst) <= EndpointCandidateRadius &&
                        GetKeyDistance(pathLast, candidateLast) <= EndpointCandidateRadius)
                    {
                        buckets.Add(bucket.Value);
                    }
                }

                return buckets;
            }

            foreach (char first in nearbyKeys[layout.GetKeyIndex(pathFirst)])
            {
                foreach (char last in nearbyKeys[layout.GetKeyIndex(pathLast)])
                {
                    if (candidatesByEndpoint.TryGetValue(GetEndpointKey(first, last), out var bucket))
                    {
                        buckets.Add(bucket);
                    }
                }
            }

            return buckets;
        }

        private int GetMinimumCandidateLength(string path)
        {
            double previousRow = 0.0;
            bool hasPreviousRow = false;
            int compressedRowLength = 0;

            foreach (char c in path)
            {
                double row = layout.GetRow(layout.GetKeyIndex(c));
                if (!hasPreviousRow || row != previousRow)
                {
                    ++compressedRowLength;
                    previousRow = row;
                    hasPreviousRow = true;
                }
            }

            return Math.Max(1, compressedRowLength - 3);
        }

        private ScoredWord ScoreWord(string path, int[] pathKeyIndexes, WordEntry wordEntry, int orderedMatchRank,
            double[] previousAlignmentRow, double[] currentAlignmentRow)
        {
            string word = wordEntry.NormalizedWord;
            double alignmentScore = GetAlignmentScore(pathKeyIndexes, word, previousAlignmentRow, currentAlignmentRow);
            double endpointScore = EndpointWeight * (
                GetKeyDistance(path[0], word[0]) +
                GetKeyDistance(path[path.Length - 1], word[word.Length - 1]));
            double lengthScore = LengthDifferenceWeight * Math.Abs(path.Length - word.Length);
            double orderedMatchScore = -GetOrderedMatchBoost(orderedMatchRank, word.Length);
            double frequencyScore = -FrequencyWeight * wordEntry.FrequencyBoost;

            double score = alignmentScore + endpointScore + lengthScore + orderedMatchScore + frequencyScore;
            return new ScoredWord(wordEntry.Word, word, score, orderedMatchRank);
        }

        private double GetAlignmentScore(int[] pathKeyIndexes, string word,
            double[] previousAlignmentRow, double[] currentAlignmentRow)
        {
            double[] previous = previousAlignmentRow;
            double[] current = currentAlignmentRow;
            double[]? keyDistances = layout.Distances;

            previous[0] = 0.0;
            for (int j = 1; j <= pathKeyIndexes.Length; ++j)
            {
                previous[j] = previous[j - 1] + ExtraPathCharacterCost;
            }

            for (int i = 1; i <= word.Length; ++i)
            {
                int wordKeyIndex = GetKeyIndex(word[i - 1]);

                // A swipe passes a doubled letter once, so a repeated letter may share the previous letter's path key.
                double missingLetterCost = i > 1 && word[i - 1] == word[i - 2] ? RepeatedLetterCost : MissingLetterCost;
                current[0] = previous[0] + missingLetterCost;
                int distanceRow = keyDistances != null && wordKeyIndex >= 0 ? wordKeyIndex * layout.Count : -1;

                for (int j = 1; j <= pathKeyIndexes.Length; ++j)
                {
                    double keyDistance = distanceRow >= 0
                        ? keyDistances![distanceRow + pathKeyIndexes[j - 1]]
                        : GetKeyDistance(wordKeyIndex, pathKeyIndexes[j - 1]);
                    double match = previous[j - 1] + keyDistance;
                    double skipPath = current[j - 1] + ExtraPathCharacterCost;
                    double missWord = previous[j] + missingLetterCost;

                    current[j] = Math.Min(Math.Min(match, skipPath), missWord);
                }

                var temp = previous;
                previous = current;
                current = temp;
            }

            return previous[pathKeyIndexes.Length];
        }

        private int GetOrderedMatchRank(string path, int[] pathKeyIndexes, string word)
        {
            bool exactEndpointMatch = path[0] == word[0] && path[path.Length - 1] == word[word.Length - 1];
            if (exactEndpointMatch && HasOrderedMatch(pathKeyIndexes, word, 0.0))
            {
                return 0;
            }

            bool nearbyEndpointMatch =
                GetKeyDistance(path[0], word[0]) <= OrderedMatchRadius &&
                GetKeyDistance(path[path.Length - 1], word[word.Length - 1]) <= OrderedMatchRadius;
            if (nearbyEndpointMatch && HasOrderedMatch(pathKeyIndexes, word, OrderedMatchRadius))
            {
                return 1;
            }

            return 2;
        }

        private static double GetOrderedMatchBoost(int orderedMatchRank, int wordLength)
        {
            if (orderedMatchRank == 0)
            {
                return ExactOrderedMatchBoost + (OrderedMatchLetterBoost * wordLength);
            }

            if (orderedMatchRank == 1)
            {
                return NearbyOrderedMatchBoost + (OrderedMatchLetterBoost * wordLength);
            }

            return 0.0;
        }

        private bool HasOrderedMatch(int[] pathKeyIndexes, string word, double radius)
        {
            int pathIndex = 0;
            for (int i = 0; i < word.Length; ++i)
            {
                // A repeated letter reuses the path key matched for the previous letter.
                if (i > 0 && word[i] == word[i - 1])
                {
                    continue;
                }

                int wordKeyIndex = GetKeyIndex(word[i]);
                bool found = false;
                while (pathIndex < pathKeyIndexes.Length)
                {
                    if (GetKeyDistance(pathKeyIndexes[pathIndex], wordKeyIndex) <= radius)
                    {
                        found = true;
                        ++pathIndex;
                        break;
                    }

                    ++pathIndex;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private double GetKeyDistance(char first, char second)
        {
            int firstIndex = layout.GetKeyIndex(first);
            int secondIndex = layout.GetKeyIndex(second);
            if (firstIndex < 0 || secondIndex < 0)
            {
                return first == second ? 0.0 : UnsupportedCharacterCost;
            }

            return layout.GetDistance(firstIndex, secondIndex);
        }

        private double GetKeyDistance(int firstIndex, int secondIndex)
        {
            if (firstIndex < 0 || secondIndex < 0)
            {
                return UnsupportedCharacterCost;
            }

            return layout.GetDistance(firstIndex, secondIndex);
        }

        private int GetKeyIndex(char value)
        {
            return layout.GetKeyIndex(value);
        }

        private int[] GetPathKeyIndexes(string path)
        {
            var indexes = new int[path.Length];
            for (int i = 0; i < path.Length; ++i)
            {
                indexes[i] = layout.GetKeyIndex(path[i]);
            }

            return indexes;
        }

        private static int GetEndpointKey(char first, char last)
        {
            return (first << 16) | last;
        }

        private string GetSupportedPath(string path)
        {
            bool containsUnsupportedCharacter = false;
            foreach (char c in path)
            {
                if (layout.GetKeyIndex(c) < 0)
                {
                    containsUnsupportedCharacter = true;
                    break;
                }
            }

            if (!containsUnsupportedCharacter)
            {
                return path;
            }

            var supportedCharacters = new char[path.Length];
            int supportedCharacterCount = 0;
            foreach (char c in path)
            {
                if (layout.GetKeyIndex(c) >= 0)
                {
                    supportedCharacters[supportedCharacterCount++] = c;
                }
            }

            return new string(supportedCharacters, 0, supportedCharacterCount);
        }

        private static char[][] CreateNearbyKeys(KeyboardLayout layout)
        {
            var nearbyKeys = new char[layout.Count][];
            for (int first = 0; first < layout.Count; ++first)
            {
                nearbyKeys[first] = Enumerable.Range(0, layout.Count)
                    .Where(second => layout.GetDistance(first, second) <= EndpointCandidateRadius)
                    .Select(layout.GetCharacter)
                    .ToArray();
            }

            return nearbyKeys;
        }

        private readonly struct ScoredWord
        {
            internal ScoredWord(string word, string normalizedWord, double score, int orderedMatchRank)
            {
                Word = word;
                NormalizedWord = normalizedWord;
                Score = score;
                OrderedMatchRank = orderedMatchRank;
            }

            internal string Word { get; }

            internal string NormalizedWord { get; }

            internal double Score { get; }

            internal int OrderedMatchRank { get; }
        }

        /// <summary>
        /// Orders candidates by match class, geometric score, edit distance, and spelling.
        /// The edit distance is computed lazily because it only breaks exact score ties.
        /// One instance belongs to one query: it owns a mutable distance workspace.
        /// </summary>
        private sealed class ScoredWordComparer : IComparer<ScoredWord>
        {
            private readonly string path;
            private readonly TextDistance.Workspace distanceWorkspace = new TextDistance.Workspace();

            internal ScoredWordComparer(string path)
            {
                this.path = path;
            }

            public int Compare(ScoredWord left, ScoredWord right)
            {
                int orderedMatchComparison = left.OrderedMatchRank.CompareTo(right.OrderedMatchRank);
                if (orderedMatchComparison != 0)
                {
                    return orderedMatchComparison;
                }

                int scoreComparison = left.Score.CompareTo(right.Score);
                if (scoreComparison != 0)
                {
                    return scoreComparison;
                }

                if (ReferenceEquals(left.NormalizedWord, right.NormalizedWord))
                {
                    return 0;
                }

                int distanceComparison = distanceWorkspace.GetDistance(path, left.NormalizedWord)
                    .CompareTo(distanceWorkspace.GetDistance(path, right.NormalizedWord));
                return distanceComparison != 0
                    ? distanceComparison
                    : string.Compare(left.Word, right.Word, StringComparison.Ordinal);
            }
        }
    }
}
