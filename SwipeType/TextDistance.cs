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
    internal static class TextDistance
    {
        internal static int GetDamerauLevenshteinDistance(this string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                return (t ?? "").Length;
            }
            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int sourceLength = s.Length;
            int targetLength = t.Length;
            int maxDistance = sourceLength + targetLength;
            var distances = new int[sourceLength + 2, targetLength + 2];
            var lastSourceRows = new Dictionary<char, int>();

            distances[0, 0] = maxDistance;
            for (int i = 0; i <= sourceLength; ++i)
            {
                distances[i + 1, 0] = maxDistance;
                distances[i + 1, 1] = i;
            }
            for (int j = 0; j <= targetLength; ++j)
            {
                distances[0, j + 1] = maxDistance;
                distances[1, j + 1] = j;
            }

            for (int i = 1; i <= sourceLength; ++i)
            {
                int lastTargetMatchColumn = 0;

                for (int j = 1; j <= targetLength; ++j)
                {
                    int lastSourceMatchRow = lastSourceRows.TryGetValue(t[j - 1], out int row) ? row : 0;
                    int substitutionCost = 1;
                    if (s[i - 1] == t[j - 1])
                    {
                        substitutionCost = 0;
                        lastTargetMatchColumn = j;
                    }

                    int substitution = distances[i, j] + substitutionCost;
                    int insertion = distances[i + 1, j] + 1;
                    int deletion = distances[i, j + 1] + 1;
                    int transposition = distances[lastSourceMatchRow, lastTargetMatchColumn]
                        + (i - lastSourceMatchRow - 1)
                        + 1
                        + (j - lastTargetMatchColumn - 1);

                    distances[i + 1, j + 1] = Math.Min(
                        Math.Min(substitution, insertion),
                        Math.Min(deletion, transposition));
                }

                lastSourceRows[s[i - 1]] = i;
            }

            return distances[sourceLength + 1, targetLength + 1];
        }
    }
}
