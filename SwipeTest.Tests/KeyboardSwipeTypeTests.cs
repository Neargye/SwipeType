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

using SwipeType;

namespace SwipeTest.Tests
{
    [TestClass]
    public class KeyboardSwipeTypeTests
    {
        [TestMethod]
        public void GetSuggestionRanksExactSwipeCandidateFirst()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(new[] { "world", "hero", "Hello", "help" });

            var suggestions = swipeType.GetSuggestion("heqerqllo", 3).ToArray();

            CollectionAssert.AreEqual(new[] { "Hello", "hero", "help" }, suggestions);
        }

        [TestMethod]
        public void GetSuggestionToleratesNearbyEndpointKeys()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(new[] { "world", "Hello", "hero" });

            var suggestions = swipeType.GetSuggestion("geqerqllp", 2).ToArray();

            CollectionAssert.AreEqual(new[] { "Hello", "hero" }, suggestions);
        }

        [TestMethod]
        public void GetSuggestionToleratesExtraPathNoise()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(new[] { "world", "hallo", "hello" });

            var suggestions = swipeType.GetSuggestion("hzzellllo", 2).ToArray();

            CollectionAssert.AreEqual(new[] { "hello", "hallo" }, suggestions);
        }

        [TestMethod]
        public void GetSuggestionCanRecoverCandidateRejectedByStrictEndpointMatch()
        {
            var words = new[] { "Hello", "hero", "world" };
            SwipeType.SwipeType keyboardSwipeType = new QwertySwipeType(words);
            SwipeType.SwipeType strictSwipeType = new MatchSwipeType(words);

            Assert.AreEqual("Hello", keyboardSwipeType.GetSuggestion("geqerqllp", 1).Single());
            Assert.AreEqual(0, strictSwipeType.GetSuggestion("geqerqllp").Count());
        }

        [TestMethod]
        public void BoundedSuggestionsMatchUnboundedTopResults()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(File.ReadAllLines("EnglishDictionary.txt"));

            var bounded = swipeType.GetSuggestion("heqerqllo", 5).ToArray();
            var unbounded = swipeType.GetSuggestion("heqerqllo").Take(5).ToArray();

            CollectionAssert.AreEqual(unbounded, bounded);
        }

        [TestMethod]
        public void GetSuggestionKeepsOriginalPathMatchesNearTopForFullDictionary()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(File.ReadAllLines("EnglishDictionary.txt"));

            AssertSuggestionRank(swipeType, "heqerqllo", "hello", 1);
            AssertSuggestionRank(swipeType, "qwertyuihgfcvbnjk", "quick", 5);
            AssertSuggestionRank(swipeType, "wertyuioiuytrtghjklkjhgfd", "world", 5);
            AssertSuggestionRank(swipeType, "dfghjioijhgvcftyuioiuytr", "doctor", 5);
            AssertSuggestionRank(swipeType, "aserfcvghjiuytedcftyuytre", "architecture", 5);
            AssertSuggestionRank(swipeType, "asdfgrtyuijhvcvghuiklkjuytyuytre", "agriculture", 5);
            AssertSuggestionRank(swipeType, "mjuytfdsdftyuiuhgvc", "music", 5);
            AssertSuggestionRank(swipeType, "vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty", "vocabulary", 5);
        }

        [TestMethod]
        public void FrequenciesBreakAmbiguousNearbyPathMatchesInFullDictionary()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(
                File.ReadAllLines("EnglishDictionary.txt"),
                new Dictionary<string, double>
                {
                    ["hello"] = 1000.0
                });

            AssertSuggestionRank(swipeType, "geqerqllp", "hello", 5);
        }

        [TestMethod]
        public void ConstructorThrowsForNullFrequencies()
        {
            AssertThrowsArgumentNullException(() => new QwertySwipeType(new[] { "hello" }, null!));
        }

        [TestMethod]
        public void FrequenciesParticipateInRanking()
        {
            SwipeType.SwipeType swipeType = new QwertySwipeType(
                new[] { "bello", "hello" },
                new Dictionary<string, double>
                {
                    ["hello"] = 1000.0
                });

            Assert.AreEqual("hello", swipeType.GetSuggestion("gello", 1).Single());
        }

        private static void AssertThrowsArgumentNullException(Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected ArgumentNullException.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        private static void AssertSuggestionRank(SwipeType.SwipeType swipeType, string path, string expectedWord, int maxRank)
        {
            var suggestions = swipeType.GetSuggestion(path, maxRank).ToArray();

            Assert.IsTrue(
                suggestions.Contains(expectedWord),
                $"Expected '{expectedWord}' in the top {maxRank} suggestions for '{path}'. Actual: {string.Join(", ", suggestions)}.");
        }
    }
}
