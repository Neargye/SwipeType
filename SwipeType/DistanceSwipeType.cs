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
    /// Ranks words by Damerau-Levenshtein edit distance.
    /// </summary>
    public class DistanceSwipeType : SwipeType
    {
        /// <summary>
        /// Initializes an edit-distance swipe suggestion provider.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public DistanceSwipeType(IEnumerable<string> wordList) : base(wordList) { }

        /// <summary>
        /// Returns suggestions for a normalized input string.
        /// </summary>
        /// <param name="input">Normalized input string.</param>
        protected override IEnumerable<string> GetSuggestionImpl(string input)
        {
            return WordEntries
                .OrderBy(x => TextDistance.GetDamerauLevenshteinDistance(input, x.NormalizedWord))
                .ThenBy(x => x.Word, StringComparer.Ordinal)
                .Select(x => x.Word);
        }
    }
}
