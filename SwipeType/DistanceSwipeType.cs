// Copyright 2018 Terik23 <neargye@gmail.com>.
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
        ///     Returns suggestions for a given inputStr.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override string[] GetSuggestion(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var inputStr = input.ToLower();
            var listSuggestion = new List<string>();

            int min = int.MaxValue;
            foreach (var s in Words)
            {
                int d = TextDistance.GetDamerauLevenshteinDistance(inputStr, s);
                int t = Math.Min(min, d);
                if (min > t)
                    listSuggestion.Clear();

                min = t;

                if (d == min)
                    listSuggestion.Add(s);
            }

            return listSuggestion.ToArray();
        }
    }
}