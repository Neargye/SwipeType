// Copyright 2018 Daniil Goncharov <neargye@gmail.com>.
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

namespace SwipeType
{
    internal static class TextDistance
    {
        // Determine the Damerau-Levensh distance between source and target
        internal static int GetDamerauLevenshteinDistance(string source, string target)
        {
            int height = source.Length + 1;
            int width = target.Length + 1;

            var matrix = new int[height, width];

            for (int h = 0; h < height; ++h)
            {
                matrix[h, 0] = h;
            }

            for (int w = 0; w < width; ++w)
            {
                matrix[0, w] = w;
            }

            for (int h = 1; h < height; ++h)
            {
                for (int w = 1; w < width; ++w)
                {
                    int cost = (source[h - 1] == target[w - 1]) ? 0 : 1;
                    int insertion = matrix[h, w - 1] + 1;
                    int deletion = matrix[h - 1, w] + 1;
                    int substitution = matrix[h - 1, w - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if ((h > 1) && (w > 1) && (source[h - 1] == target[w - 2]) && (source[h - 2] == target[w - 1]))
                    {
                        distance = Math.Min(distance, matrix[h - 2, w - 2] + cost);
                    }

                    matrix[h, w] = distance;
                }
            }

            return matrix[height - 1, width - 1];
        }
    }
}