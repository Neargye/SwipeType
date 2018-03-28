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
using System.Linq;
using System.Text;

namespace SwipeType
{
    /// <summary>
    ///     Simple SwipeType.
    /// </summary>
    public class SimpleSwipeType : SwipeType
    {
        /// <summary>
        ///     Keyboard layout.
        /// </summary>
        public static readonly string[] KeyboardLayoutEnglish =
        {
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm"
        };

        /// <summary>
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public SimpleSwipeType(string[] wordList) : base(wordList) { }

        /// <summary>
        ///     Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns></returns>
        public override string[] GetSuggestion(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return GetSuggestionHelper(input).ToArray();
        }

        /// <summary>
        ///     Returns suggestions for an input string.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string[] GetSuggestion(string input, int count)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return GetSuggestionHelper(input).Take(count).ToArray();
        }

        private IEnumerable<string> GetSuggestionHelper(string input)
        {
            string inputStr = input.ToLower();
            return Words
                .Where(x => (!string.IsNullOrEmpty(x)) && (x[0] == inputStr[0]) && (x[x.Length > 0 ? x.Length - 1 : 0] == inputStr[inputStr.Length > 0 ? inputStr.Length - 1 : 0]))
                .Where(x => Match(inputStr, x))
                .Where(x => x.Length > GetMinimumWordlength(inputStr))
                .OrderBy(x => TextDistance.GetDamerauLevenshteinDistance(inputStr, x));
        }

        /// <summary>
        ///     Checks if a letter is present in a path or not.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="word"></param>
        private static bool Match(string path, string word)
        {
            int i = 0;
            foreach (char c in path)
            {
                if (c == word[i])
                    ++i;
                if (i == word.Length)
                    return true;
            }

            return i == word.Length;
        }

        /// <summary>
        ///     Returns the row number of the character.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private int GetKeyboardRow(char c)
        {
            for (int i = 0; i < KeyboardLayoutEnglish.Length; ++i)
                if (KeyboardLayoutEnglish[i].Contains(c))
                    return i;

            return -1;
        }

        /// <summary>
        ///     Removes redundant sequential characters.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private static StringBuilder Compress(StringBuilder sequence)
        {
            // Example: 11123311 => 1231.

            if (sequence == null || sequence.Length == 0)
                return new StringBuilder();

            var s = new StringBuilder();
            s.Append(sequence[0]);

            for (int i = 1; i < sequence.Length; ++i)
                if (s[s.Length - 1] != sequence[i])
                    s.Append(sequence[i]);

            return s;
        }

        /// <summary>
        ///     Returns the minimum possible word length from the path.
        ///     Uses the number of transitions from different rows in
        ///     the keyboard layout to determin the minimum length.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private int GetMinimumWordlength(string path)
        {
            var rowNumbers = new StringBuilder();
            foreach (char inChar in path)
            {
                int i = GetKeyboardRow(inChar);
                if (i >= 0)
                    rowNumbers.Append(i);
            }

            var compressedRowNumbers = Compress(rowNumbers);
            return compressedRowNumbers.Length - 3;
        }
    }
}