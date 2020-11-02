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
using System.Linq;

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
        /// Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        protected override IEnumerable<string> GetSuggestionImpl(string input)
        {
            return Words.OrderBy(x => TextDistance.GetDamerauLevenshteinDistance(input, x));
        }
    }
}
