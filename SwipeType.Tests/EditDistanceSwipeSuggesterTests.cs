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
    public class EditDistanceSwipeSuggesterTests
    {
        [TestMethod]
        public void GetSuggestionsRanksClosestWordsFirst()
        {
            var suggester = new EditDistanceSwipeSuggester(new[] { "world", "Hello", "hero" });

            CollectionAssert.AreEqual(new[] { "Hello", "hero", "world" }, suggester.GetSuggestions("hello", 3).ToArray());
        }

        [TestMethod]
        public void FrequenciesOrderWordsWithEqualDistance()
        {
            var words = new[] { "aab", "aac", "aad" };
            var frequencies = new Dictionary<string, double> { ["aad"] = 100.0, ["aac"] = 10.0, ["not-in-the-dictionary"] = 1e9 };

            // Each word is at distance 1 from "aax"; without frequencies ordinal spelling decides.
            CollectionAssert.AreEqual(new[] { "aab", "aac", "aad" }, new EditDistanceSwipeSuggester(words).GetSuggestions("aax", 3).ToArray());
            CollectionAssert.AreEqual(new[] { "aad", "aac" }, new EditDistanceSwipeSuggester(words, frequencies).GetSuggestions("aax", 2).ToArray());
        }

        [TestMethod]
        public void FrequenciesKeepTheirOrderAcrossExtremeScales()
        {
            // A logarithmic weight relative to double.MaxValue rounds double.Epsilon to zero; the raw value does not.
            var frequencies = new Dictionary<string, double> { ["b"] = double.Epsilon, ["zz"] = double.MaxValue };

            CollectionAssert.AreEqual(
                new[] { "b", "a", "zz" },
                new EditDistanceSwipeSuggester(new[] { "a", "b", "zz" }, frequencies).GetSuggestions("c", 3).ToArray());
        }

        [TestMethod]
        public void OversizedDistanceComparisonsThrowInsufficientMemory()
        {
            string longWord = new string('a', 50000);
            var suggester = new EditDistanceSwipeSuggester(new[] { longWord });

            Assert.AreEqual(longWord, suggester.GetSuggestions(longWord, 1).Single());
            Assert.ThrowsExactly<InsufficientMemoryException>(() => suggester.GetSuggestions(new string('b', 50000), 1));
        }

        [TestMethod]
        public void LengthPruningKeepsTiesThatCanWinByOrdinalSpelling()
        {
            var suggester = new EditDistanceSwipeSuggester(new[] { "b", "aa" });

            Assert.AreEqual("aa", suggester.GetSuggestions("a", 1).Single());
            CollectionAssert.AreEqual(new[] { "aa", "b" }, suggester.GetSuggestions("a", 2).ToArray());
        }

        [TestMethod]
        public void BoundedRankingMatchesAnIndependentOracle()
        {
            const string letters = "abcdefghij";
            var random = new Random(9271);
            string[] words = TestData.CreateWords(random, letters, 500, 1, 15);
            var frequencies = words.Where((_, index) => index % 4 == 0).ToDictionary(word => word, word => (double)(word[0] % 3));
            var suggester = new EditDistanceSwipeSuggester(words, frequencies);

            for (int iteration = 0; iteration < 30; ++iteration)
            {
                string path = TestData.CreateString(random, letters, random.Next(1, 18));
                string[] expected = words
                    .OrderBy(word => TestData.Distance(path, word))
                    .ThenByDescending(word => frequencies.TryGetValue(word, out double frequency) ? frequency : 0.0)
                    .ThenBy(word => word, StringComparer.Ordinal)
                    .ToArray();

                foreach (int count in new[] { 1, 3, 10, 100, int.MaxValue })
                {
                    CollectionAssert.AreEqual(
                        expected.Take(count).ToArray(),
                        suggester.GetSuggestions(path, count).ToArray(),
                        $"Path '{path}', count {count}.");
                }
            }
        }
    }
}
