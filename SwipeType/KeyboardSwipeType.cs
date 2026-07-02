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

namespace SwipeType
{
    /// <summary>
    /// Base class for SwipeType implementations that rank words by keyboard geometry.
    /// </summary>
    public abstract class KeyboardSwipeType : SwipeType
    {
        private const double MissingLetterCost = 1.35;
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
        private readonly IReadOnlyDictionary<string, double> frequencyBoosts;
        private readonly IReadOnlyDictionary<char, KeyPoint> keyboard;

        /// <summary>
        /// Initializes a keyboard-geometry swipe suggestion provider.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        /// <param name="keyboardLayout">Keyboard layout used for geometric scoring.</param>
        protected KeyboardSwipeType(IEnumerable<string> wordList, IEnumerable<Key> keyboardLayout)
            : this(wordList, keyboardLayout, new Dictionary<string, double>()) { }

        /// <summary>
        /// Initializes a keyboard-geometry swipe suggestion provider with word frequencies.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        /// <param name="keyboardLayout">Keyboard layout used for geometric scoring.</param>
        /// <param name="wordFrequencies">Optional word frequency weights. Higher values improve ranking.</param>
        protected KeyboardSwipeType(
            IEnumerable<string> wordList,
            IEnumerable<Key> keyboardLayout,
            IReadOnlyDictionary<string, double> wordFrequencies) : base(wordList)
        {
            if (keyboardLayout == null)
            {
                throw new ArgumentNullException(nameof(keyboardLayout));
            }

            if (wordFrequencies == null)
            {
                throw new ArgumentNullException(nameof(wordFrequencies));
            }

            keyboard = CreateKeyboard(keyboardLayout);
            candidatesByEndpoint = WordEntries
                .GroupBy(x => GetEndpointKey(x.NormalizedWord[0], x.NormalizedWord[x.NormalizedWord.Length - 1]))
                .ToDictionary(x => x.Key, x => x.ToArray());
            frequencyBoosts = CreateFrequencyBoosts(wordFrequencies);
        }

        /// <summary>
        /// Returns suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        protected override IEnumerable<string> GetSuggestionImpl(string input)
        {
            return GetCandidates(input)
                .Select(x => ScoreWord(input, x))
                .OrderBy(x => x.Score)
                .ThenBy(x => x.Distance)
                .ThenBy(x => x.Word, StringComparer.Ordinal)
                .Select(x => x.Word);
        }

        /// <summary>
        /// Returns a bounded number of suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        /// <param name="count">The number of elements to return.</param>
        protected override IEnumerable<string> GetSuggestionImpl(string input, int count)
        {
            return GetTopSuggestions(input, count).Select(x => x.Word);
        }

        private IEnumerable<ScoredWord> GetTopSuggestions(string input, int count)
        {
            var top = new List<ScoredWord>(Math.Min(count, WordEntries.Count));
            foreach (var candidate in GetCandidates(input))
            {
                var scoredWord = ScoreWord(input, candidate);
                if (top.Count < count)
                {
                    InsertSorted(top, scoredWord);
                }
                else if (Compare(scoredWord, top[top.Count - 1]) < 0)
                {
                    top.RemoveAt(top.Count - 1);
                    InsertSorted(top, scoredWord);
                }
            }

            return top;
        }

        private IEnumerable<WordEntry> GetCandidates(string input)
        {
            var endpointCandidates = GetEndpointCandidates(input).ToArray();
            if (endpointCandidates.Length == 0)
            {
                return endpointCandidates;
            }

            int minimumLength = GetMinimumCandidateLength(input);
            int maximumLength = input.Length + 2;
            var lengthCandidates = endpointCandidates
                .Where(x => x.NormalizedWord.Length >= minimumLength && x.NormalizedWord.Length <= maximumLength)
                .ToArray();

            return lengthCandidates.Length == 0 ? endpointCandidates : lengthCandidates;
        }

        private IEnumerable<WordEntry> GetEndpointCandidates(string input)
        {
            if (!keyboard.ContainsKey(input[0]) || !keyboard.ContainsKey(input[input.Length - 1]))
            {
                return WordEntries;
            }

            var candidates = new List<WordEntry>();
            foreach (char first in GetNearbyKeys(input[0]))
            {
                foreach (char last in GetNearbyKeys(input[input.Length - 1]))
                {
                    if (candidatesByEndpoint.TryGetValue(GetEndpointKey(first, last), out var bucket))
                    {
                        candidates.AddRange(bucket);
                    }
                }
            }

            return candidates;
        }

        private int GetMinimumCandidateLength(string path)
        {
            double previousRow = 0.0;
            bool hasPreviousRow = false;
            int compressedRowLength = 0;

            foreach (char c in path)
            {
                if (!keyboard.TryGetValue(c, out var keyPoint))
                {
                    continue;
                }

                if (!hasPreviousRow || keyPoint.Y != previousRow)
                {
                    ++compressedRowLength;
                    previousRow = keyPoint.Y;
                    hasPreviousRow = true;
                }
            }

            return Math.Max(1, compressedRowLength - 3);
        }

        private ScoredWord ScoreWord(string input, WordEntry wordEntry)
        {
            int orderedMatchRank = GetOrderedMatchRank(input, wordEntry.NormalizedWord);

            return new ScoredWord(
                wordEntry.Word,
                GetSwipePathScore(input, wordEntry.NormalizedWord, orderedMatchRank),
                TextDistance.GetDamerauLevenshteinDistance(input, wordEntry.NormalizedWord),
                orderedMatchRank);
        }

        private double GetSwipePathScore(string path, string word, int orderedMatchRank)
        {
            if (word.Length == 0)
            {
                return 0.0;
            }

            double alignmentScore = GetAlignmentScore(path, word);
            double endpointScore = EndpointWeight * (
                GetKeyDistance(path[0], word[0]) +
                GetKeyDistance(path[path.Length - 1], word[word.Length - 1]));
            double lengthScore = LengthDifferenceWeight * Math.Abs(path.Length - word.Length);
            double orderedMatchScore = -GetOrderedMatchBoost(orderedMatchRank, word.Length);
            double frequencyScore = GetFrequencyScore(word);

            return alignmentScore + endpointScore + lengthScore + orderedMatchScore + frequencyScore;
        }

        private double GetAlignmentScore(string path, string word)
        {
            var previous = new double[path.Length + 1];
            var current = new double[path.Length + 1];

            for (int j = 1; j <= path.Length; ++j)
            {
                previous[j] = previous[j - 1] + ExtraPathCharacterCost;
            }

            for (int i = 1; i <= word.Length; ++i)
            {
                current[0] = previous[0] + MissingLetterCost;

                for (int j = 1; j <= path.Length; ++j)
                {
                    double match = previous[j - 1] + GetKeyDistance(word[i - 1], path[j - 1]);
                    double skipPath = current[j - 1] + ExtraPathCharacterCost;
                    double missWord = previous[j] + MissingLetterCost;

                    current[j] = Math.Min(Math.Min(match, skipPath), missWord);
                }

                var temp = previous;
                previous = current;
                current = temp;
            }

            return previous[path.Length];
        }

        private int GetOrderedMatchRank(string path, string word)
        {
            bool exactEndpointMatch = path[0] == word[0] && path[path.Length - 1] == word[word.Length - 1];
            if (exactEndpointMatch && HasOrderedMatch(path, word, 0.0))
            {
                return 0;
            }

            bool nearbyEndpointMatch =
                GetKeyDistance(path[0], word[0]) <= OrderedMatchRadius &&
                GetKeyDistance(path[path.Length - 1], word[word.Length - 1]) <= OrderedMatchRadius;
            if (nearbyEndpointMatch && HasOrderedMatch(path, word, OrderedMatchRadius))
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

        private bool HasOrderedMatch(string path, string word, double radius)
        {
            int pathIndex = 0;
            foreach (char c in word)
            {
                bool found = false;
                while (pathIndex < path.Length)
                {
                    if (GetKeyDistance(path[pathIndex], c) <= radius)
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
            if (!keyboard.TryGetValue(first, out var firstPoint) || !keyboard.TryGetValue(second, out var secondPoint))
            {
                return first == second ? 0.0 : UnsupportedCharacterCost;
            }

            double deltaX = firstPoint.X - secondPoint.X;
            double deltaY = firstPoint.Y - secondPoint.Y;

            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        private static int GetEndpointKey(char first, char last)
        {
            return (first << 16) | last;
        }

        private IEnumerable<char> GetNearbyKeys(char key)
        {
            foreach (var pair in keyboard)
            {
                if (GetKeyDistance(key, pair.Key) <= EndpointCandidateRadius)
                {
                    yield return pair.Key;
                }
            }
        }

        private double GetFrequencyScore(string word)
        {
            return frequencyBoosts.TryGetValue(word, out double boost) ? -FrequencyWeight * boost : 0.0;
        }

        private static IReadOnlyDictionary<string, double> CreateFrequencyBoosts(IReadOnlyDictionary<string, double> wordFrequencies)
        {
            var frequencies = new Dictionary<string, double>(StringComparer.Ordinal);
            double maxFrequency = 0.0;
            foreach (var pair in wordFrequencies)
            {
                double frequency = Math.Max(0.0, pair.Value);
                if (frequency <= 0.0)
                {
                    continue;
                }

                string word = NormalizeWord(pair.Key);
                if (!frequencies.TryGetValue(word, out double existingFrequency) || frequency > existingFrequency)
                {
                    frequencies[word] = frequency;
                    maxFrequency = Math.Max(maxFrequency, frequency);
                }
            }

            if (maxFrequency <= 0.0)
            {
                return frequencies;
            }

            var boosts = new Dictionary<string, double>(StringComparer.Ordinal);
            double maxLogFrequency = Math.Log(1.0 + maxFrequency);
            foreach (var pair in frequencies)
            {
                boosts[pair.Key] = Math.Log(1.0 + pair.Value) / maxLogFrequency;
            }

            return boosts;
        }

        private static void InsertSorted(List<ScoredWord> scoredWords, ScoredWord scoredWord)
        {
            int index = 0;
            while (index < scoredWords.Count && Compare(scoredWords[index], scoredWord) <= 0)
            {
                ++index;
            }

            scoredWords.Insert(index, scoredWord);
        }

        private static int Compare(ScoredWord left, ScoredWord right)
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

            int distanceComparison = left.Distance.CompareTo(right.Distance);
            return distanceComparison != 0
                ? distanceComparison
                : string.Compare(left.Word, right.Word, StringComparison.Ordinal);
        }

        private static Dictionary<char, KeyPoint> CreateKeyboard(IEnumerable<Key> keyboardLayout)
        {
            var keyboard = new Dictionary<char, KeyPoint>();
            foreach (var key in keyboardLayout)
            {
                keyboard[NormalizeWord(key.Value.ToString())[0]] = new KeyPoint(key.X, key.Y);
            }

            return keyboard;
        }

        /// <summary>
        /// A key's normalized character and position in keyboard-layout coordinates.
        /// </summary>
        protected readonly struct Key
        {
            /// <summary>
            /// Initializes a keyboard key.
            /// </summary>
            public Key(char value, double x, double y)
            {
                Value = value;
                X = x;
                Y = y;
            }

            /// <summary>
            /// Key character.
            /// </summary>
            public char Value { get; }

            /// <summary>
            /// Horizontal key coordinate.
            /// </summary>
            public double X { get; }

            /// <summary>
            /// Vertical key coordinate.
            /// </summary>
            public double Y { get; }
        }

        private sealed class ScoredWord
        {
            internal ScoredWord(string word, double score, int distance, int orderedMatchRank)
            {
                Word = word;
                Score = score;
                Distance = distance;
                OrderedMatchRank = orderedMatchRank;
            }

            internal string Word { get; }

            internal double Score { get; }

            internal int Distance { get; }

            internal int OrderedMatchRank { get; }
        }

        private readonly struct KeyPoint
        {
            internal KeyPoint(double x, double y)
            {
                X = x;
                Y = y;
            }

            internal double X { get; }

            internal double Y { get; }
        }
    }
}
