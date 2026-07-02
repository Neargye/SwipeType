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
    /// Base class for swipe suggestion providers.
    /// </summary>
    public abstract class SwipeType
    {
        /// <summary>
        /// Initializes a swipe suggestion provider with a dictionary of words.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        protected SwipeType(IEnumerable<string> wordList)
        {
            if (wordList == null)
            {
                throw new ArgumentNullException(nameof(wordList));
            }

            var words = wordList
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Select(word => word.Trim())
                .ToArray();

            Words = Array.AsReadOnly(words);
            WordEntries = Array.AsReadOnly(words
                .Select(word => new WordEntry(word, NormalizeWord(word)))
                .ToArray());
        }

        /// <summary>
        /// Gets the dictionary words used for suggestions.
        /// </summary>
        public IReadOnlyList<string> Words { get; }

        /// <summary>
        /// Dictionary words paired with their normalized lookup form.
        /// </summary>
        protected IReadOnlyList<WordEntry> WordEntries { get; }

        /// <summary>
        /// Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string.</param>
        public IEnumerable<string> GetSuggestion(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Enumerable.Empty<string>();
            }

            return GetSuggestionImpl(NormalizeWord(input));
        }

        /// <summary>
        /// Returns a bounded number of suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="count">The number of elements to return.</param>
        public IEnumerable<string> GetSuggestion(string input, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be zero or greater.");
            }

            if (string.IsNullOrWhiteSpace(input) || count == 0)
            {
                return Enumerable.Empty<string>();
            }

            return GetSuggestionImpl(NormalizeWord(input), count);
        }

        /// <summary>
        /// Returns suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        protected abstract IEnumerable<string> GetSuggestionImpl(string input);

        /// <summary>
        /// Returns a bounded number of suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        /// <param name="count">The number of elements to return.</param>
        protected virtual IEnumerable<string> GetSuggestionImpl(string input, int count)
        {
            return GetSuggestionImpl(input).Take(count);
        }

        /// <summary>
        /// Normalizes a dictionary word or input string for lookup.
        /// </summary>
        protected static string NormalizeWord(string value)
        {
            return value.Trim().ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Dictionary word with its normalized lookup form.
        /// </summary>
        protected sealed class WordEntry
        {
            internal WordEntry(string word, string normalizedWord)
            {
                Word = word;
                NormalizedWord = normalizedWord;
            }

            /// <summary>
            /// Original dictionary word.
            /// </summary>
            public string Word { get; }

            /// <summary>
            /// Normalized lookup form.
            /// </summary>
            public string NormalizedWord { get; }
        }
    }
}
