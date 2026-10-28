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

namespace SwipeType
{
    internal static class TextDistance
    {
        /// <summary>
        /// Reusable storage for unrestricted Damerau-Levenshtein distance. Not thread-safe.
        /// </summary>
        internal sealed class Workspace
        {
            private const int AsciiCharacterCount = 128;

            private int[] distances = Array.Empty<int>();
            private readonly int[] lastAsciiSourceRows = new int[AsciiCharacterCount];
            private readonly Dictionary<char, int> lastOtherSourceRows = new Dictionary<char, int>();
            private int stride;

            private void EnsureCapacity(int sourceLength, int targetLength)
            {
                long rowCount = (long)sourceLength + 2;
                long columnCount = (long)targetLength + 2;
                long cellCount = rowCount * columnCount;
                if (cellCount > int.MaxValue)
                {
                    throw new InsufficientMemoryException("The swipe path and a candidate word are too long to compare.");
                }

                stride = (int)columnCount;
                if (cellCount > distances.Length)
                {
                    long capacity = Math.Max(cellCount, Math.Min(2L * distances.Length, int.MaxValue));
                    distances = new int[(int)capacity];
                }
            }

            internal int GetDistance(string source, string target)
            {
                return GetDistance(source, target, int.MaxValue);
            }

            /// <summary>
            /// Returns the exact distance up to <paramref name="maxDistance"/>, or a value above that bound.
            /// </summary>
            internal int GetDistance(string source, string target, int maxDistance)
            {
                if (string.IsNullOrEmpty(source))
                {
                    return (target ?? "").Length;
                }

                if (string.IsNullOrEmpty(target))
                {
                    return source.Length;
                }

                if (string.Equals(source, target, StringComparison.Ordinal))
                {
                    return 0;
                }

                int sourceLength = source.Length;
                int targetLength = target.Length;
                if (Math.Abs(sourceLength - targetLength) > maxDistance)
                {
                    return maxDistance + 1;
                }

                EnsureCapacity(sourceLength, targetLength);
                int sentinel = sourceLength + targetLength;
                Array.Clear(lastAsciiSourceRows, 0, lastAsciiSourceRows.Length);
                lastOtherSourceRows.Clear();

                SetDistance(0, 0, sentinel);
                for (int i = 0; i <= sourceLength; ++i)
                {
                    SetDistance(i + 1, 0, sentinel);
                    SetDistance(i + 1, 1, i);
                }

                for (int j = 0; j <= targetLength; ++j)
                {
                    SetDistance(0, j + 1, sentinel);
                    SetDistance(1, j + 1, j);
                }

                for (int i = 1; i <= sourceLength; ++i)
                {
                    int lastTargetMatchColumn = 0;
                    int rowMinimum = i;

                    for (int j = 1; j <= targetLength; ++j)
                    {
                        int lastSourceMatchRow = GetLastSourceRow(target[j - 1]);
                        int previousTargetMatchColumn = lastTargetMatchColumn;
                        int substitutionCost = 1;
                        if (source[i - 1] == target[j - 1])
                        {
                            substitutionCost = 0;
                            lastTargetMatchColumn = j;
                        }

                        int substitution = GetDistance(i, j) + substitutionCost;
                        int insertion = GetDistance(i + 1, j) + 1;
                        int deletion = GetDistance(i, j + 1) + 1;
                        int transposition = GetDistance(lastSourceMatchRow, previousTargetMatchColumn)
                            + (i - lastSourceMatchRow - 1)
                            + 1
                            + (j - previousTargetMatchColumn - 1);

                        int distance = Math.Min(Math.Min(substitution, insertion), Math.Min(deletion, transposition));
                        SetDistance(i + 1, j + 1, distance);
                        if (distance < rowMinimum)
                        {
                            rowMinimum = distance;
                        }
                    }

                    // Row minima cannot decrease, even through transpositions: skipping from an earlier row
                    // costs at least as much as reaching this row by deletions. The final distance therefore
                    // cannot fall below this minimum.
                    if (rowMinimum > maxDistance)
                    {
                        return maxDistance + 1;
                    }

                    SetLastSourceRow(source[i - 1], i);
                }

                return GetDistance(sourceLength + 1, targetLength + 1);
            }

            private int GetLastSourceRow(char value)
            {
                if (value < AsciiCharacterCount)
                {
                    return lastAsciiSourceRows[value];
                }

                return lastOtherSourceRows.TryGetValue(value, out int row) ? row : 0;
            }

            private void SetLastSourceRow(char value, int row)
            {
                if (value < AsciiCharacterCount)
                {
                    lastAsciiSourceRows[value] = row;
                }
                else
                {
                    lastOtherSourceRows[value] = row;
                }
            }

            private int GetDistance(int row, int column)
            {
                return distances[(row * stride) + column];
            }

            private void SetDistance(int row, int column, int value)
            {
                distances[(row * stride) + column] = value;
            }
        }
    }
}
