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
        internal static int GetDamerauLevenshteinDistance(string source, string target, int max = -1)
        {
            int maxLength = Math.Max(source.Length, target.Length);
            int[] currentRow = new int[maxLength + 1];
            int[] previousRow = new int[maxLength + 1];
            int[] transpositionRow = new int[maxLength + 1];

            int firstLength = source.Length;
            int secondLength = target.Length;

            if (firstLength == 0)
            {
                return secondLength;
            }
            if (secondLength == 0)
            {
                return firstLength;
            }

            if (firstLength > secondLength)
            {
                string tmp = source;
                source = target;
                target = tmp;
                firstLength = secondLength;
                secondLength = target.Length;
            }

            if (max < 0)
            {
                max = secondLength;
            }
            if (secondLength - firstLength > max)
            {
                return max + 1;
            }

            for (int i = 0; i <= firstLength; i++)
            {
                previousRow[i] = i;
            }

            char lastSecondCh = (char)0;
            for (int i = 1; i <= secondLength; i++)
            {
                char secondCh = target[i - 1];
                currentRow[0] = i;
                int from = Math.Max(i - max - 1, 1);
                int to = Math.Min(i + max + 1, firstLength);

                char lastFirstCh = (char)0;
                for (int j = from; j <= to; j++)
                {
                    char firstCh = source[j - 1];
                    int cost = firstCh == secondCh ? 0 : 1;
                    int value = Math.Min(Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1), previousRow[j - 1] + cost);
                    if (firstCh == lastSecondCh && secondCh == lastFirstCh)
                    {
                        value = Math.Min(value, transpositionRow[j - 2] + cost);
                    }
                    currentRow[j] = value;
                    lastFirstCh = firstCh;
                }
                lastSecondCh = secondCh;

                int[] tempRow = transpositionRow;
                transpositionRow = previousRow;
                previousRow = currentRow;
                currentRow = tempRow;
            }

            return previousRow[firstLength];
        }
    }
}
