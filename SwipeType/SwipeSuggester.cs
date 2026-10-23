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
using System.Globalization;
using System.Linq;

namespace SwipeType
{
    /// <summary>
    /// Base class for swipe suggesters: turns a swipe path into ranked dictionary words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A suggester copies and preprocesses its dictionary in the constructor and is immutable afterwards.
    /// One instance can serve any number of concurrent <see cref="GetSuggestions(string, int)"/> calls.
    /// </para>
    /// <para>
    /// The class cannot be derived from outside this library. Use it as the common type for the built-in
    /// suggesters: <see cref="GeometricSwipeSuggester"/>, <see cref="StrictSwipeSuggester"/>, and
    /// <see cref="EditDistanceSwipeSuggester"/>.
    /// </para>
    /// </remarks>
    public abstract class SwipeSuggester
    {
        internal static readonly IReadOnlyDictionary<string, double> NoFrequencies = new Dictionary<string, double>();

        /// <summary>
        /// Copies, normalizes, and deduplicates the dictionary and converts frequencies into ranking weights.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="words"/> or <paramref name="wordFrequencies"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency value is negative, NaN, or infinite.</exception>
        private protected SwipeSuggester(IEnumerable<string> words, IReadOnlyDictionary<string, double> wordFrequencies)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            if (wordFrequencies == null)
            {
                throw new ArgumentNullException(nameof(wordFrequencies));
            }

            var entries = new List<WordEntry>();
            var normalizedWords = new HashSet<string>(StringComparer.Ordinal);
            foreach (string word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                string trimmedWord = word.Trim();
                string normalizedWord = NormalizeWord(trimmedWord);
                if (normalizedWords.Add(normalizedWord))
                {
                    entries.Add(new WordEntry(trimmedWord, normalizedWord));
                }
            }

            ApplyFrequencyBoosts(entries, normalizedWords, wordFrequencies);
            Words = Array.AsReadOnly(entries.Select(x => x.Word).ToArray());
            WordEntries = entries.ToArray();
        }

        /// <summary>
        /// Gets the dictionary words this suggester can suggest: blank entries removed, surrounding whitespace
        /// trimmed, and case-insensitive duplicates removed (the first spelling is kept).
        /// </summary>
        public IReadOnlyList<string> Words { get; }

        internal WordEntry[] WordEntries { get; }

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> dictionary words ranked from best to worst match
        /// for a swipe path.
        /// </summary>
        /// <param name="swipePath">
        /// The characters of the keys the swipe passed over, in order. Matching is case-insensitive and
        /// surrounding whitespace is ignored.
        /// </param>
        /// <param name="maxCount">
        /// The maximum number of suggestions to return. Use <see cref="int.MaxValue"/> for the full ranking.
        /// </param>
        /// <returns>
        /// At most <paramref name="maxCount"/> words from <see cref="Words"/>; empty when the path is blank,
        /// <paramref name="maxCount"/> is zero, or no word is a candidate. For the same path, a smaller
        /// <paramref name="maxCount"/> always returns a prefix of the result for a larger one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="swipePath"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        /// <exception cref="InsufficientMemoryException">
        /// The path and a candidate word are too long to compare by edit distance, which needs
        /// (path length + 2) × (word length + 2) cells of memory; the limit is <see cref="int.MaxValue"/> cells.
        /// </exception>
        public IReadOnlyList<string> GetSuggestions(string swipePath, int maxCount)
        {
            if (swipePath == null)
            {
                throw new ArgumentNullException(nameof(swipePath));
            }

            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "The count must be zero or greater.");
            }

            if (maxCount == 0 || string.IsNullOrWhiteSpace(swipePath))
            {
                return Array.Empty<string>();
            }

            return GetSuggestionsCore(NormalizeWord(swipePath), maxCount);
        }

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> ranked words for a non-blank normalized path.
        /// </summary>
        private protected abstract IReadOnlyList<string> GetSuggestionsCore(string path, int maxCount);

        /// <summary>
        /// Creates a bounded candidate buffer that exposes its current worst result for safe pruning.
        /// </summary>
        private protected TopCandidateBuffer<T> CreateTopCandidateBuffer<T>(int count, IComparer<T> comparer)
        {
            return new TopCandidateBuffer<T>(count, comparer, Math.Min(count, WordEntries.Length));
        }

        /// <summary>
        /// Normalizes a dictionary word or swipe path for lookup.
        /// </summary>
        internal static string NormalizeWord(string value)
        {
            return value.Trim().ToLower(CultureInfo.InvariantCulture);
        }

        private static void ApplyFrequencyBoosts(
            IReadOnlyList<WordEntry> entries, HashSet<string> dictionaryWords,
            IReadOnlyDictionary<string, double> wordFrequencies)
        {
            var frequencies = new Dictionary<string, double>(StringComparer.Ordinal);
            double maxFrequency = 0.0;
            foreach (var pair in wordFrequencies)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    throw new ArgumentException("Frequency keys must not be null or whitespace.", nameof(wordFrequencies));
                }

                double frequency = pair.Value;
                if (double.IsNaN(frequency) || double.IsInfinity(frequency) || frequency < 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(wordFrequencies), frequency,
                        $"Frequency for '{pair.Key}' must be finite and zero or greater.");
                }

                if (frequency == 0.0)
                {
                    continue;
                }

                string word = NormalizeWord(pair.Key);
                if (!dictionaryWords.Contains(word))
                {
                    continue;
                }

                if (!frequencies.TryGetValue(word, out double existingFrequency) || frequency > existingFrequency)
                {
                    frequencies[word] = frequency;
                    maxFrequency = Math.Max(maxFrequency, frequency);
                }
            }

            if (maxFrequency <= 0.0)
            {
                return;
            }

            double maxLogFrequency = LogOnePlus(maxFrequency);
            foreach (WordEntry entry in entries)
            {
                if (frequencies.TryGetValue(entry.NormalizedWord, out double frequency))
                {
                    entry.Frequency = frequency;
                    entry.FrequencyBoost = LogOnePlus(frequency) / maxLogFrequency;
                }
            }
        }

        private static double LogOnePlus(double value)
        {
            // Avoid rounding 1 + a tiny positive frequency to 1 (and then dividing 0 by 0).
            return value < 1e-8 ? value * (1.0 - (0.5 * value)) : Math.Log(1.0 + value);
        }

        private static void SiftWorstCandidateUp<T>(List<T> candidates, int index, IComparer<T> comparer)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (comparer.Compare(candidates[parentIndex], candidates[index]) >= 0)
                {
                    break;
                }

                T parent = candidates[parentIndex];
                candidates[parentIndex] = candidates[index];
                candidates[index] = parent;
                index = parentIndex;
            }
        }

        private static void SiftWorstCandidateDown<T>(List<T> candidates, IComparer<T> comparer)
        {
            int index = 0;
            while (true)
            {
                int leftIndex = (2 * index) + 1;
                if (leftIndex >= candidates.Count)
                {
                    return;
                }

                int rightIndex = leftIndex + 1;
                int worstChildIndex = rightIndex < candidates.Count && comparer.Compare(candidates[leftIndex], candidates[rightIndex]) < 0
                    ? rightIndex
                    : leftIndex;
                if (comparer.Compare(candidates[index], candidates[worstChildIndex]) >= 0)
                {
                    return;
                }

                T current = candidates[index];
                candidates[index] = candidates[worstChildIndex];
                candidates[worstChildIndex] = current;
                index = worstChildIndex;
            }
        }

        /// <summary>
        /// Maintains a max-heap containing the best bounded candidate set.
        /// </summary>
        private protected sealed class TopCandidateBuffer<T>
        {
            private readonly List<T> candidates;
            private readonly IComparer<T> comparer;
            private readonly int count;

            internal TopCandidateBuffer(int count, IComparer<T> comparer, int capacity)
            {
                if (count <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
                }

                this.count = count;
                this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
                candidates = new List<T>(capacity);
            }

            internal bool IsFull => candidates.Count == count;

            internal T Worst
            {
                get
                {
                    return candidates.Count == count
                        ? candidates[0]
                        : throw new InvalidOperationException("The top-candidate buffer is not full.");
                }
            }

            internal void Add(T candidate)
            {
                if (candidates.Count < count)
                {
                    candidates.Add(candidate);
                    SiftWorstCandidateUp(candidates, candidates.Count - 1, comparer);
                }
                else if (comparer.Compare(candidate, candidates[0]) < 0)
                {
                    candidates[0] = candidate;
                    SiftWorstCandidateDown(candidates, comparer);
                }
            }

            internal List<T> FinalizeSortedCandidates()
            {
                candidates.Sort(comparer);
                return candidates;
            }
        }

        /// <summary>
        /// Dictionary word with its normalized lookup form and frequency.
        /// </summary>
        internal sealed class WordEntry
        {
            internal WordEntry(string word, string normalizedWord)
            {
                Word = word;
                NormalizedWord = normalizedWord;
                int collapsedLength = 1;
                for (int i = 1; i < normalizedWord.Length; ++i)
                {
                    if (normalizedWord[i] != normalizedWord[i - 1])
                    {
                        ++collapsedLength;
                    }
                }

                CollapsedLength = collapsedLength;
            }

            /// <summary>
            /// Original dictionary word.
            /// </summary>
            internal string Word { get; }

            /// <summary>
            /// Normalized lookup form.
            /// </summary>
            internal string NormalizedWord { get; }

            /// <summary>
            /// Length of the normalized word with runs of equal letters counted once, as a swipe passes them.
            /// </summary>
            internal int CollapsedLength { get; }

            /// <summary>
            /// Supplied frequency; zero when none was supplied.
            /// </summary>
            internal double Frequency { get; set; }

            /// <summary>
            /// Logarithmic frequency weight in [0, 1]; zero when no frequency was supplied.
            /// </summary>
            internal double FrequencyBoost { get; set; }
        }
    }
}
