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

using System.Globalization;

namespace SwipeType.Tests
{
    /// <summary>
    /// The contract shared by every suggester.
    /// </summary>
    [TestClass]
    public class SwipeSuggesterTests
    {
        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void ConstructorRejectsNullArguments(string kind)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => TestData.CreateSuggester(kind, null!));
            Assert.ThrowsExactly<ArgumentNullException>(() => TestData.CreateSuggester(kind, new[] { "a" }, null!));
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void ConstructorRejectsInvalidFrequencies(string kind)
        {
            foreach (double frequency in new[] { -1.0, double.NaN, double.NegativeInfinity, double.PositiveInfinity })
            {
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => TestData.CreateSuggester(kind, new[] { "hello" }, new Dictionary<string, double> { ["hello"] = frequency }));
            }

            Assert.ThrowsExactly<ArgumentException>(
                () => TestData.CreateSuggester(kind, new[] { "hello" }, new Dictionary<string, double> { [" "] = 1.0 }));
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void ConstructorCopiesTrimsAndDeduplicatesTheDictionary(string kind)
        {
            var words = new[] { "", "  ", null!, " Hello ", "hello", "HELLO", "hero" };
            SwipeSuggester suggester = TestData.CreateSuggester(kind, words);

            words[3] = "zero";

            CollectionAssert.AreEqual(new[] { "Hello", "hero" }, suggester.Words.ToArray());
        }

        [TestMethod]
        public void NormalizationIgnoresTheCurrentCulture()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                Assert.AreEqual("istanbul", SwipeSuggester.NormalizeWord(" ISTANBUL "));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void FrequencyKeysThatDifferOnlyInCaseUseTheLargestValue(string kind)
        {
            // "haello" is one edit from both words, so frequency decides; without it "hallo" sorts first.
            var lowFirst = new Dictionary<string, double> { ["hello"] = 1, ["HELLO"] = 1000, ["hallo"] = 500 };
            var highFirst = new Dictionary<string, double> { ["HELLO"] = 1000, ["hello"] = 1, ["hallo"] = 500 };

            foreach (var frequencies in new[] { lowFirst, highFirst })
            {
                SwipeSuggester suggester = TestData.CreateSuggester(kind, new[] { "hallo", "hello" }, frequencies);

                CollectionAssert.AreEqual(new[] { "hello", "hallo" }, suggester.GetSuggestions("haello", 2).ToArray());
            }
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void GetSuggestionsValidatesArgumentsAndHandlesEmptyRequests(string kind)
        {
            SwipeSuggester suggester = TestData.CreateSuggester(kind, new[] { "hello", "hero" });

            Assert.ThrowsExactly<ArgumentNullException>(() => suggester.GetSuggestions(null!, 1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => suggester.GetSuggestions("hello", -1));
            Assert.AreEqual(0, suggester.GetSuggestions("hello", 0).Count);
            Assert.AreEqual(0, suggester.GetSuggestions(" \t ", 5).Count);
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void GetSuggestionsMatchesCaseInsensitivelyAndKeepsOriginalSpelling(string kind)
        {
            SwipeSuggester suggester = TestData.CreateSuggester(kind, new[] { "Hello", "world" });

            Assert.AreEqual("Hello", suggester.GetSuggestions("  HELLO ", 1).Single());
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void SmallerCountsReturnPrefixesOfTheFullRanking(string kind)
        {
            var random = new Random(1847);
            string[] words = TestData.CreateWords(random, TestData.QwertyLetters, 400, 1, 10);
            var frequencies = words
                .Where((_, index) => index % 7 == 0)
                .Select((word, index) => (word, frequency: (double)(index % 5)))
                .ToDictionary(x => x.word, x => x.frequency, StringComparer.Ordinal);
            SwipeSuggester suggester = TestData.CreateSuggester(kind, words, frequencies);

            for (int iteration = 0; iteration < 40; ++iteration)
            {
                // Matching endpoints keep candidates eligible while the random middle varies match quality.
                string word = words[random.Next(words.Length)];
                string path = word[0] + TestData.CreateString(random, TestData.QwertyLetters, random.Next(0, 20)) + word[^1];
                IReadOnlyList<string> full = suggester.GetSuggestions(path, int.MaxValue);
                foreach (int count in new[] { 1, 2, 5, 20, 500 })
                {
                    CollectionAssert.AreEqual(
                        full.Take(count).ToArray(),
                        suggester.GetSuggestions(path, count).ToArray(),
                        $"Path '{path}', count {count}.");
                }
            }
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        [DataRow(nameof(EditDistanceSwipeSuggester))]
        public void ConcurrentQueriesReturnTheSameResultsAsSequentialQueries(string kind)
        {
            var random = new Random(4242);
            string[] words = TestData.CreateWords(random, TestData.QwertyLetters, 3000, 2, 9);
            string[] paths = Enumerable
                .Range(0, 64)
                .Select(_ => TestData.CreateString(random, TestData.QwertyLetters, random.Next(2, 16)))
                .ToArray();
            SwipeSuggester suggester = TestData.CreateSuggester(kind, words);
            string[] expected = paths.Select(path => string.Join(",", suggester.GetSuggestions(path, 10))).ToArray();

            var actual = new string[paths.Length * 8];
            Parallel.For(0, actual.Length, i => actual[i] = string.Join(",", suggester.GetSuggestions(paths[i % paths.Length], 10)));

            for (int i = 0; i < actual.Length; ++i)
            {
                Assert.AreEqual(expected[i % paths.Length], actual[i], $"Path '{paths[i % paths.Length]}'.");
            }
        }

        [TestMethod]
        [DataRow(nameof(GeometricSwipeSuggester))]
        [DataRow(nameof(StrictSwipeSuggester))]
        public void RejectedLongWordsDoNotRequireADistanceMatrix(string kind)
        {
            SwipeSuggester suggester = TestData.CreateSuggester(kind, new[] { new string('p', 50000) });

            Assert.AreEqual(0, suggester.GetSuggestions(new string('a', 50000), int.MaxValue).Count);
        }
    }
}
