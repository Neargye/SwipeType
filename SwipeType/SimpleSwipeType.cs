using System;
using System.Linq;
using System.Text;

namespace SwipeType
{
    /// <summary>
    ///     Simple SwipeType.
    /// </summary>
    public class SimpleSwipeType
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
        ///     Dictionary of words.
        /// </summary>
        public string[] Words { get; }

        /// <summary>
        /// </summary>
        /// <param name="wordList">The dictionary of words.</param>
        public SimpleSwipeType(string[] wordList)
        {
            Words = wordList;
        }

        /// <summary>
        ///     Returns suggestions for a given inputStr.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string[] GetSuggestion(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            string inputStr = input.ToLower();
            return Words
                .Where(x => (!string.IsNullOrEmpty(x)) && (x[0] == inputStr[0]) && (x[x.Length > 0 ? x.Length - 1 : 0] == inputStr[inputStr.Length > 0 ? inputStr.Length - 1 : 0]))
                .Where(x => Match(inputStr, x))
                .Where(x => x.Length > GetMinimumWordlength(inputStr))
                .ToArray();
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

            throw new ArgumentException($"The char should be of {KeyboardLayoutEnglish} layout type.");
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

            StringBuilder s = new StringBuilder();
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
            StringBuilder rowNumbers = new StringBuilder();
            foreach (char inChar in path)
                rowNumbers.Append(GetKeyboardRow(inChar));

            StringBuilder compressedRowNumbers = Compress(rowNumbers);
            return compressedRowNumbers.Length - 3;
        }
    }
}