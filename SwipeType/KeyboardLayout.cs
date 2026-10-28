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
    /// <summary>
    /// An immutable keyboard layout: the positions of the keys a swipe path can pass over.
    /// </summary>
    /// <remarks>
    /// One unit of <see cref="KeyboardKey.X"/> is the distance between neighboring keys in a row; keys with the
    /// same <see cref="KeyboardKey.Y"/> form a row. Key characters are matched case-insensitively. A layout is
    /// validated once and can be shared by any number of suggesters and threads.
    /// </remarks>
    public sealed class KeyboardLayout
    {
        private const int MaximumCachedKeyCount = 256;

        private readonly Dictionary<char, int> keyIndexes = new Dictionary<char, int>();
        private readonly KeyboardKey[] keys;

        /// <summary>
        /// Initializes a layout from its keys.
        /// </summary>
        /// <param name="keys">The keys of the layout.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="keys"/> is empty, contains a whitespace key, or contains duplicate keys after lower-casing.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">A key coordinate is NaN or infinite.</exception>
        public KeyboardLayout(IEnumerable<KeyboardKey> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var keyList = new List<KeyboardKey>();
            foreach (var key in keys)
            {
                if (double.IsNaN(key.X) || double.IsInfinity(key.X) ||
                    double.IsNaN(key.Y) || double.IsInfinity(key.Y))
                {
                    throw new ArgumentOutOfRangeException(nameof(keys), "Keyboard coordinates must be finite.");
                }

                // Swipe paths are trimmed, so a whitespace key could never start or end a path.
                if (char.IsWhiteSpace(key.Character))
                {
                    throw new ArgumentException("Keyboard keys must not be whitespace characters.", nameof(keys));
                }

                char normalizedKey = char.ToLowerInvariant(key.Character);
                if (keyIndexes.ContainsKey(normalizedKey))
                {
                    throw new ArgumentException($"Keyboard layout contains duplicate key '{normalizedKey}'.", nameof(keys));
                }

                keyIndexes.Add(normalizedKey, keyList.Count);
                keyList.Add(key);
            }

            if (keyList.Count == 0)
            {
                throw new ArgumentException("Keyboard layout must contain at least one key.", nameof(keys));
            }

            this.keys = keyList.ToArray();
            Keys = Array.AsReadOnly(this.keys);
            Distances = this.keys.Length <= MaximumCachedKeyCount ? CreateDistances(this.keys) : null;
        }

        /// <summary>
        /// Gets the 26-letter English QWERTY layout: rows at <c>Y</c> = 0, 1, 2 with horizontal offsets 0, 0.5,
        /// and 1. Use its <see cref="Keys"/> as a starting point for other layouts.
        /// </summary>
        public static KeyboardLayout Qwerty { get; } = CreateQwerty();

        /// <summary>
        /// Gets the keys of the layout in their original order and spelling.
        /// </summary>
        public IReadOnlyList<KeyboardKey> Keys { get; }

        internal int Count => keys.Length;

        /// <summary>
        /// Row-major distances between keys by index, or null for large layouts, which compute them on demand
        /// to bound the quadratic memory.
        /// </summary>
        internal double[]? Distances { get; }

        /// <summary>
        /// Returns the index of a lower-case key character, or -1 when the key is not on the layout.
        /// </summary>
        internal int GetKeyIndex(char character)
        {
            return keyIndexes.TryGetValue(character, out int index) ? index : -1;
        }

        internal char GetCharacter(int keyIndex)
        {
            return char.ToLowerInvariant(keys[keyIndex].Character);
        }

        internal double GetRow(int keyIndex)
        {
            return keys[keyIndex].Y;
        }

        internal double GetDistance(int firstIndex, int secondIndex)
        {
            return Distances == null
                ? CalculateDistance(keys[firstIndex], keys[secondIndex])
                : Distances[(firstIndex * keys.Length) + secondIndex];
        }

        private static double[] CreateDistances(KeyboardKey[] keys)
        {
            var distances = new double[keys.Length * keys.Length];
            for (int first = 0; first < keys.Length; ++first)
            {
                for (int second = 0; second < keys.Length; ++second)
                {
                    distances[(first * keys.Length) + second] = CalculateDistance(keys[first], keys[second]);
                }
            }

            return distances;
        }

        private static double CalculateDistance(KeyboardKey first, KeyboardKey second)
        {
            double deltaX = first.X - second.X;
            double deltaY = first.Y - second.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        private static KeyboardLayout CreateQwerty()
        {
            var keys = new List<KeyboardKey>();
            AddRow(keys, "qwertyuiop", 0.0, 0.0);
            AddRow(keys, "asdfghjkl", 0.5, 1.0);
            AddRow(keys, "zxcvbnm", 1.0, 2.0);

            return new KeyboardLayout(keys);
        }

        private static void AddRow(List<KeyboardKey> keys, string row, double xOffset, double y)
        {
            for (int i = 0; i < row.Length; ++i)
            {
                keys.Add(new KeyboardKey(row[i], i + xOffset, y));
            }
        }
    }
}
