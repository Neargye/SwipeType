// Copyright 2018 - 2020 Daniil Goncharov <neargye@gmail.com>.
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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SwipeType
{
    /// <summary>
    /// Abstract SwipeType.
    /// </summary>
    public abstract class SwipeType
    {
        /// <summary>
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        protected SwipeType(string[] wordList) { Words = wordList; }

        /// <summary>
        /// Dictionary of words.
        /// </summary>
        public string[] Words { get; }

        /// <summary>
        /// Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        public IEnumerable<string> GetSuggestion(string input)
        {
            return GetSuggestion(input, -1);
        }

        /// <summary>
        /// Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="count">The number of elements to return.</param>
        public IEnumerable<string> GetSuggestion(string input, int count)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Enumerable.Empty<string>();
            }
            var suggestion = GetSuggestionImpl(input.ToLower(CultureInfo.InvariantCulture));
            if (count > 0)
            {
                return suggestion.Take(count);
            }
            return suggestion;
        }

        /// <summary>
        /// Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        protected abstract IEnumerable<string> GetSuggestionImpl(string input);
    }
}
