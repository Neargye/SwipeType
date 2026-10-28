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
    /// Ranks words by Damerau-Levenshtein distance without a keyboard layout.
    /// </summary>
    /// <remarks>
    /// Ties are resolved by frequency, then ordinal spelling. Queries may scan the whole dictionary.
    /// </remarks>
    public sealed class EditDistanceSwipeSuggester : SwipeSuggester
    {
        private readonly LengthBucket[] lengthBuckets;

        /// <summary>
        /// Initializes an edit-distance suggester.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <exception cref="ArgumentNullException"><paramref name="words"/> is null.</exception>
        public EditDistanceSwipeSuggester(IEnumerable<string> words)
            : this(words, NoFrequencies) { }

        /// <summary>
        /// Initializes an edit-distance suggester with word frequencies.
        /// </summary>
        /// <param name="words">The dictionary of words. Blank entries are ignored.</param>
        /// <param name="wordFrequencies">
        /// Word frequencies, matched case-insensitively, that order words with equal edit distance.
        /// All values must be finite and non-negative; valid entries outside <paramref name="words"/> do not affect ranking.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="words"/> or <paramref name="wordFrequencies"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">A frequency key is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A frequency is negative, NaN, or infinite.</exception>
        public EditDistanceSwipeSuggester(IEnumerable<string> words, IReadOnlyDictionary<string, double> wordFrequencies)
            : base(words, wordFrequencies)
        {
            lengthBuckets = WordEntries
                .GroupBy(x => x.NormalizedWord.Length)
                .OrderBy(x => x.Key)
                .Select(x => new LengthBucket(x.Key, x.ToArray()))
                .ToArray();
        }

        private protected override IReadOnlyList<string> GetSuggestionsCore(string path, int maxCount)
        {
            var workspace = new TextDistance.Workspace();
            var top = CreateTopCandidateBuffer(maxCount, EditDistanceCandidate.Comparer);
            int rightIndex = FindFirstLengthAtLeast(path.Length);
            int leftIndex = rightIndex - 1;

            // Visit length buckets from the nearest length outwards. The length difference is a lower bound
            // of the edit distance, so once it exceeds the current worst result no later bucket can enter.
            while (leftIndex >= 0 || rightIndex < lengthBuckets.Length)
            {
                int leftDistance = leftIndex >= 0
                    ? path.Length - lengthBuckets[leftIndex].Length
                    : int.MaxValue;
                int rightDistance = rightIndex < lengthBuckets.Length
                    ? lengthBuckets[rightIndex].Length - path.Length
                    : int.MaxValue;
                int bucketIndex = leftDistance <= rightDistance ? leftIndex-- : rightIndex++;
                int minimumDistance = Math.Min(leftDistance, rightDistance);
                if (top.IsFull && minimumDistance > top.Worst.Distance)
                {
                    break;
                }

                foreach (WordEntry wordEntry in lengthBuckets[bucketIndex].Words)
                {
                    // Distances beyond the worst result need not be exact; ties still matter.
                    int maxDistance = top.IsFull ? top.Worst.Distance : int.MaxValue;
                    top.Add(new EditDistanceCandidate(wordEntry.Word, workspace.GetDistance(path, wordEntry.NormalizedWord, maxDistance), wordEntry.Frequency));
                }
            }

            return top.FinalizeSortedCandidates().Select(x => x.Word).ToArray();
        }

        private int FindFirstLengthAtLeast(int length)
        {
            int first = 0;
            int count = lengthBuckets.Length;
            while (count > 0)
            {
                int step = count / 2;
                int index = first + step;
                if (lengthBuckets[index].Length < length)
                {
                    first = index + 1;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }

            return first;
        }

        private readonly struct LengthBucket
        {
            internal LengthBucket(int length, WordEntry[] words)
            {
                Length = length;
                Words = words;
            }

            internal int Length { get; }

            internal WordEntry[] Words { get; }
        }
    }
}
