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

using System.Collections.Generic;

namespace SwipeType
{
    /// <summary>
    /// Ranks words by English QWERTY keyboard geometry and swipe-path proximity.
    /// </summary>
    public class QwertySwipeType : KeyboardSwipeType
    {
        private static readonly Key[] KeyboardLayout = CreateKeyboardLayout();

        /// <summary>
        /// Initializes a QWERTY swipe suggestion provider.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public QwertySwipeType(IEnumerable<string> wordList) : base(wordList, KeyboardLayout) { }

        /// <summary>
        /// Initializes a QWERTY swipe suggestion provider with word frequencies.
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        /// <param name="wordFrequencies">Optional word frequency weights. Higher values improve ranking.</param>
        public QwertySwipeType(IEnumerable<string> wordList, IReadOnlyDictionary<string, double> wordFrequencies)
            : base(wordList, KeyboardLayout, wordFrequencies) { }

        private static Key[] CreateKeyboardLayout()
        {
            var keys = new List<Key>();
            AddKeyboardRow(keys, "qwertyuiop", 0.0, 0.0);
            AddKeyboardRow(keys, "asdfghjkl", 0.5, 1.0);
            AddKeyboardRow(keys, "zxcvbnm", 1.0, 2.0);

            return keys.ToArray();
        }

        private static void AddKeyboardRow(List<Key> keys, string row, double xOffset, double y)
        {
            for (int i = 0; i < row.Length; ++i)
            {
                keys.Add(new Key(row[i], i + xOffset, y));
            }
        }
    }
}
