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

namespace SwipeType
{
    /// <summary>
    /// A word ranked by edit distance, then frequency, then ordinal spelling.
    /// </summary>
    internal readonly struct EditDistanceCandidate
    {
        internal static readonly IComparer<EditDistanceCandidate> Comparer = Comparer<EditDistanceCandidate>.Create(Compare);

        internal EditDistanceCandidate(string word, int distance, double frequency)
        {
            Word = word;
            Distance = distance;
            Frequency = frequency;
        }

        internal string Word { get; }

        internal int Distance { get; }

        internal double Frequency { get; }

        private static int Compare(EditDistanceCandidate left, EditDistanceCandidate right)
        {
            int distanceComparison = left.Distance.CompareTo(right.Distance);
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            int frequencyComparison = right.Frequency.CompareTo(left.Frequency);
            return frequencyComparison != 0
                ? frequencyComparison
                : string.Compare(left.Word, right.Word, StringComparison.Ordinal);
        }
    }
}
