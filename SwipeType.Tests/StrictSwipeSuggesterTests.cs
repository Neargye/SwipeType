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

namespace SwipeType.Tests
{
    [TestClass]
    public class StrictSwipeSuggesterTests
    {
        [TestMethod]
        public void GetSuggestionsFindsExpectedWordsFromTheDictionaryFile()
        {
            var suggester = new StrictSwipeSuggester(TestData.EnglishDictionary);

            CollectionAssert.AreEqual(new[] { "hello", "hero", "ho" }, suggester.GetSuggestions("heqerqllo", 3).ToArray());
        }

        [TestMethod]
        public void DoubledLettersNeedOnlyOnePass()
        {
            var suggester = new StrictSwipeSuggester(new[] { "hello", "hero", "heel", "hollo" });

            CollectionAssert.AreEqual(new[] { "hello", "hero" }, suggester.GetSuggestions("heqerqlo", 5).ToArray());
        }

        [TestMethod]
        public void GetSuggestionsIncludesWordsAtTheMinimumLengthBoundary()
        {
            var suggester = new StrictSwipeSuggester(new[] { "an", "am", "as" });

            CollectionAssert.AreEqual(new[] { "an" }, suggester.GetSuggestions("aqzan", 3).ToArray());
        }

        [TestMethod]
        public void FrequenciesOrderWordsWithEqualDistance()
        {
            // Both words match the path "abcd" in order and are one letter shorter than it.
            var words = new[] { "abd", "acd" };

            Assert.AreEqual("abd", new StrictSwipeSuggester(words).GetSuggestions("abcd", 1).Single());
            Assert.AreEqual(
                "acd",
                new StrictSwipeSuggester(words, new Dictionary<string, double> { ["acd"] = 5.0 }).GetSuggestions("abcd", 1).Single());

            // Extreme scales keep their order: double.Epsilon still beats no frequency.
            CollectionAssert.AreEqual(
                new[] { "acd", "abd" },
                new StrictSwipeSuggester(
                    new[] { "abd", "acd", "zz" },
                    new Dictionary<string, double> { ["acd"] = double.Epsilon, ["zz"] = double.MaxValue })
                    .GetSuggestions("abcd", 2).ToArray());
        }

        [TestMethod]
        public void CustomLayoutRowsBoundTheShortestWord()
        {
            // Five row runs in "abcda" make the heuristic reject words shorter than two letters.
            var layout = new KeyboardLayout("abcd".Select((c, row) => new KeyboardKey(c, 0.0, row)));
            var suggester = new StrictSwipeSuggester(new[] { "a", "aa", "aba", "ada" }, layout);

            CollectionAssert.AreEqual(new[] { "aba", "ada", "aa" }, suggester.GetSuggestions("abcda", 5).ToArray());
            CollectionAssert.AreEqual(new[] { "a" }, new StrictSwipeSuggester(new[] { "a" }).GetSuggestions("abcda", 5).ToArray());
            Assert.ThrowsExactly<ArgumentNullException>(() => new StrictSwipeSuggester(new[] { "abc" }, (KeyboardLayout)null!));
        }

        [TestMethod]
        public void RankingMatchesAnIndependentOracle()
        {
            const string letters = "qwertasdfgzxcv";
            var random = new Random(3307);
            string[] words = TestData.CreateWords(random, letters, 600, 1, 7);
            var frequencies = words.Where((_, index) => index % 5 == 0).ToDictionary(word => word, word => (double)(word.Length % 3));
            var suggester = new StrictSwipeSuggester(words, frequencies);
            var rows = KeyboardLayout.Qwerty.Keys.ToDictionary(key => key.Character, key => key.Y);

            for (int iteration = 0; iteration < 60; ++iteration)
            {
                string word = words[random.Next(words.Length)];
                string path = word[0] + TestData.CreateString(random, letters, random.Next(0, 12)) + word[^1];
                int compressedRows = path.Select(c => rows[c]).Where((row, i) => i == 0 || row != rows[path[i - 1]]).Count();
                string[] expected = words
                    .Where(w => w[0] == path[0] && w[^1] == path[^1] && w.Length >= compressedRows - 3 && IsSubsequence(CollapseRepeats(w), path))
                    .OrderBy(w => TestData.Distance(path, w))
                    .ThenByDescending(w => frequencies.TryGetValue(w, out double frequency) ? frequency : 0.0)
                    .ThenBy(w => w, StringComparer.Ordinal)
                    .ToArray();

                CollectionAssert.AreEqual(expected, suggester.GetSuggestions(path, int.MaxValue).ToArray(), $"Path '{path}'.");
                CollectionAssert.AreEqual(expected.Take(3).ToArray(), suggester.GetSuggestions(path, 3).ToArray(), $"Path '{path}'.");
            }
        }

        private static string CollapseRepeats(string word)
        {
            return new string(word.Where((c, i) => i == 0 || c != word[i - 1]).ToArray());
        }

        private static bool IsSubsequence(string word, string path)
        {
            int index = 0;
            foreach (char c in path)
            {
                if (index < word.Length && word[index] == c)
                {
                    ++index;
                }
            }

            return index == word.Length;
        }
    }
}
