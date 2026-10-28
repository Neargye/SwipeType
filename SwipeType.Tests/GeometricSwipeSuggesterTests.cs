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
    public class GeometricSwipeSuggesterTests
    {
        [TestMethod]
        public void GetSuggestionsRanksExactSwipeCandidateFirst()
        {
            var suggester = new GeometricSwipeSuggester(new[] { "world", "hero", "Hello", "help" });

            CollectionAssert.AreEqual(new[] { "Hello", "hero", "help" }, suggester.GetSuggestions("heqerqllo", 3).ToArray());
        }

        [TestMethod]
        public void GetSuggestionsToleratesNearbyEndpointKeys()
        {
            var words = new[] { "world", "Hello", "hero" };

            CollectionAssert.AreEqual(new[] { "Hello", "hero" }, new GeometricSwipeSuggester(words).GetSuggestions("geqerqllp", 2).ToArray());
            Assert.AreEqual(0, new StrictSwipeSuggester(words).GetSuggestions("geqerqllp", 2).Count);
        }

        [TestMethod]
        public void GetSuggestionsToleratesExtraPathNoise()
        {
            var suggester = new GeometricSwipeSuggester(new[] { "world", "hallo", "hello" });

            CollectionAssert.AreEqual(new[] { "hello", "hallo" }, suggester.GetSuggestions("hzzellllo", 2).ToArray());
        }

        [TestMethod]
        public void GetSuggestionsIgnoresCharactersOutsideTheLayout()
        {
            var suggester = new GeometricSwipeSuggester(new[] { "world", "Hello", "hero", "help" });

            CollectionAssert.AreEqual(
                suggester.GetSuggestions("heqerqllo", 3).ToArray(),
                suggester.GetSuggestions("1heqer-qllo!", 3).ToArray());
            Assert.AreEqual(0, suggester.GetSuggestions("123-!", 5).Count);
        }

        [TestMethod]
        public void OrderedMatchClassesTakePriorityOverScore()
        {
            // "zhoe" has the exact endpoints of the path but its letters are not in path order.
            var suggester = new GeometricSwipeSuggester(new[] { "zone", "she", "se", "zhoe" });

            CollectionAssert.AreEqual(new[] { "zone", "she", "se", "zhoe" }, suggester.GetSuggestions("zohze", 4).ToArray());
        }

        [TestMethod]
        public void DoubledLettersNeedOnlyOnePass()
        {
            AssertInTop(new GeometricSwipeSuggester(TestData.EnglishDictionary), "heqerqlo", "hello", 3);
            CollectionAssert.AreEqual(
                new[] { "hello", "halo" },
                new GeometricSwipeSuggester(new[] { "halo", "hello" }).GetSuggestions("heqerqlo", 2).ToArray());

            // Both words are exact ordered matches; the alignment must not charge the second "t" as a missing letter.
            CollectionAssert.AreEqual(
                new[] { "letter", "leer" },
                new GeometricSwipeSuggester(new[] { "leer", "letter" }).GetSuggestions("lkjhytrertrer", 2).ToArray());
        }

        [TestMethod]
        public void LengthFilterCountsDoubledLettersOnce()
        {
            // "bookkeeper" collapses to the 7 letters of "bokeper"; a shorter word that fits must not push it out.
            CollectionAssert.AreEqual(
                new[] { "bokeper", "bookkeeper" },
                new GeometricSwipeSuggester(new[] { "bokeper", "bookkeeper" }).GetSuggestions("bokeper", int.MaxValue).ToArray());
            CollectionAssert.AreEqual(
                new[] { "abc", "aabbcc" },
                new GeometricSwipeSuggester(new[] { "abc", "aabbcc" }).GetSuggestions("abc", int.MaxValue).ToArray());

            // The bound is two letters beyond the path.
            CollectionAssert.AreEqual(
                new[] { "abcdef", "abcdxyef" },
                new GeometricSwipeSuggester(new[] { "abcdef", "abcdxyef", "abcdxyzef" }).GetSuggestions("abcdef", int.MaxValue).ToArray());
        }

        [TestMethod]
        public void ShorterWordWinsAnIdenticalPathUnlessFrequenciesSayOtherwise()
        {
            var words = new[] { "too", "to" };

            CollectionAssert.AreEqual(new[] { "to", "too" }, new GeometricSwipeSuggester(words).GetSuggestions("tyuio", 2).ToArray());
            CollectionAssert.AreEqual(
                new[] { "too", "to" },
                new GeometricSwipeSuggester(words, new Dictionary<string, double> { ["too"] = 10.0, ["to"] = 1.0 })
                    .GetSuggestions("tyuio", 2).ToArray());
        }

        [TestMethod]
        public void SwipeCorpusWordsRankNearTheTopWithTheFullDictionary()
        {
            var suggester = new GeometricSwipeSuggester(TestData.EnglishDictionary);

            foreach (string line in File.ReadLines("SwipePaths.tsv"))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                string[] columns = line.Split('\t');
                Assert.AreEqual(2, columns.Length, $"Invalid swipe corpus row: '{line}'.");
                AssertInTop(suggester, columns[1], columns[0], columns[0] == "hello" ? 1 : 5);
            }
        }

        [TestMethod]
        public void FrequenciesReorderWordsWithinAMatchClass()
        {
            // Both words are nearby ordered matches for "gello"; "hello" is geometrically closer.
            var words = new[] { "bello", "hello" };

            Assert.AreEqual("hello", new GeometricSwipeSuggester(words).GetSuggestions("gello", 1).Single());
            Assert.AreEqual(
                "bello",
                new GeometricSwipeSuggester(words, new Dictionary<string, double> { ["BELLO"] = 1000.0 }).GetSuggestions("gello", 1).Single());
            AssertInTop(
                new GeometricSwipeSuggester(TestData.EnglishDictionary, new Dictionary<string, double> { ["hello"] = 1000.0 }),
                "geqerqllp", "hello", 5);
        }

        [TestMethod]
        public void FrequenciesDoNotOverrideAnExactOrderedMatch()
        {
            var suggester = new GeometricSwipeSuggester(new[] { "hello", "gello" }, new Dictionary<string, double> { ["gello"] = 1000.0 });

            CollectionAssert.AreEqual(new[] { "hello", "gello" }, suggester.GetSuggestions("hello", 2).ToArray());
        }

        [TestMethod]
        [DataRow(1e-20, 2e-20)]
        [DataRow(double.Epsilon, 2 * double.Epsilon)]
        public void TinyPositiveFrequenciesPreserveRelativeRanking(double lower, double higher)
        {
            // 'f' and 'h' are equally far from 'g', so without frequencies ordinal spelling puts "fello" first.
            var words = new[] { "fello", "hello" };
            var suggester = new GeometricSwipeSuggester(words, new Dictionary<string, double> { ["fello"] = lower, ["hello"] = higher });

            CollectionAssert.AreEqual(new[] { "fello", "hello" }, new GeometricSwipeSuggester(words).GetSuggestions("gello", 2).ToArray());
            CollectionAssert.AreEqual(new[] { "hello", "fello" }, suggester.GetSuggestions("gello", 2).ToArray());
        }

        [TestMethod]
        public void FrequenciesForWordsOutsideTheDictionaryDoNotDiluteBoosts()
        {
            // If the huge out-of-dictionary value set the scale, "bello" would get a tiny boost and stay second.
            var suggester = new GeometricSwipeSuggester(
                new[] { "bello", "hello" },
                new Dictionary<string, double> { ["bello"] = 1000.0, ["not-in-the-dictionary"] = double.MaxValue });

            Assert.AreEqual("bello", suggester.GetSuggestions("gello", 1).Single());
        }

        [TestMethod]
        public void DefaultLayoutIsQwerty()
        {
            var words = new[] { "hello", "hero", "world", "help" };
            var copiedQwerty = new KeyboardLayout(KeyboardLayout.Qwerty.Keys);

            CollectionAssert.AreEqual(
                new GeometricSwipeSuggester(words).GetSuggestions("heqerqllo", int.MaxValue).ToArray(),
                new GeometricSwipeSuggester(words, copiedQwerty).GetSuggestions("heqerqllo", int.MaxValue).ToArray());
            Assert.ThrowsExactly<ArgumentNullException>(() => new GeometricSwipeSuggester(words, (KeyboardLayout)null!));
        }

        [TestMethod]
        public void CustomLayoutDrivesGeometricRanking()
        {
            // 'x' sits right next to 'a' on this layout, so the path "axz" is closest to "az".
            var layout = new[]
            {
                new KeyboardKey('a', 0.0, 0.0),
                new KeyboardKey('x', 0.5, 0.0),
                new KeyboardKey('z', 5.0, 0.0),
                new KeyboardKey('b', 5.5, 0.0)
            };
            var suggester = new GeometricSwipeSuggester(new[] { "ab", "az" }, new KeyboardLayout(layout));

            Assert.AreEqual("az", suggester.GetSuggestions("axz", 1).Single());
        }

        [TestMethod]
        public void EditDistanceBreaksExactScoreTies()
        {
            // 'b' and 'c' share a position, so "abd" and "acd" have identical geometric scores for any path.
            // Ordinal spelling would put "abd" first; the edit distance to the path must win.
            var layout = new[]
            {
                new KeyboardKey('a', 0.0, 0.0),
                new KeyboardKey('b', 1.0, 0.0),
                new KeyboardKey('c', 1.0, 0.0),
                new KeyboardKey('d', 2.0, 0.0)
            };
            var suggester = new GeometricSwipeSuggester(new[] { "abd", "acd" }, new KeyboardLayout(layout));

            CollectionAssert.AreEqual(new[] { "acd", "abd" }, suggester.GetSuggestions("acd", 2).ToArray());
            CollectionAssert.AreEqual(new[] { "acd" }, suggester.GetSuggestions("acd", 1).ToArray());
        }

        [TestMethod]
        public void LargeLayoutsUseTheSameSuggestionContract()
        {
            KeyboardKey[] keys = Enumerable
                .Range(0, 300)
                .Select(index => new KeyboardKey((char)('\uE000' + index), index * 2.0, 0.0))
                .ToArray();
            string word = new string(new[] { keys[0].Character, keys[^1].Character });
            var suggester = new GeometricSwipeSuggester(new[] { word }, new KeyboardLayout(keys));

            Assert.AreEqual(word, suggester.GetSuggestions(word, 1).Single());
        }

        [TestMethod]
        public void CachedAndOnDemandKeyDistancesProduceTheSameOrdering()
        {
            // 256 keys use the precomputed distance table; 257 keys compute distances on demand.
            KeyboardKey[] cachedLayout = CreateGridLayout(256);
            KeyboardKey[] onDemandLayout = CreateGridLayout(257);
            char first = cachedLayout[0].Character;
            char last = cachedLayout[255].Character;
            string[] words =
            {
                new string(new[] { first, cachedLayout[17].Character, cachedLayout[34].Character, last }),
                new string(new[] { first, cachedLayout[1].Character, cachedLayout[18].Character, last }),
                new string(new[] { first, cachedLayout[16].Character, cachedLayout[32].Character, last }),
                new string(new[] { first, cachedLayout[85].Character, cachedLayout[170].Character, last }),
                new string(new[] { cachedLayout[1].Character, cachedLayout[200].Character, cachedLayout[239].Character })
            };
            string path = new string(new[]
            {
                first, cachedLayout[1].Character, cachedLayout[17].Character, cachedLayout[18].Character,
                cachedLayout[34].Character, cachedLayout[51].Character, last
            });
            var cached = new GeometricSwipeSuggester(words, new KeyboardLayout(cachedLayout));
            var onDemand = new GeometricSwipeSuggester(words, new KeyboardLayout(onDemandLayout));

            foreach (int count in new[] { 1, 3, int.MaxValue })
            {
                CollectionAssert.AreEqual(cached.GetSuggestions(path, count).ToArray(), onDemand.GetSuggestions(path, count).ToArray());
            }

            Assert.AreEqual(5, cached.GetSuggestions(path, int.MaxValue).Count);
        }

        private static void AssertInTop(SwipeSuggester suggester, string path, string expectedWord, int maxRank)
        {
            IReadOnlyList<string> suggestions = suggester.GetSuggestions(path, maxRank);

            Assert.IsTrue(
                suggestions.Contains(expectedWord),
                $"Expected '{expectedWord}' in the top {maxRank} suggestions for '{path}'. Actual: {string.Join(", ", suggestions)}.");
        }

        private static KeyboardKey[] CreateGridLayout(int count)
        {
            return Enumerable
                .Range(0, count)
                .Select(index => new KeyboardKey((char)('\uE000' + index), index % 16, index / 16))
                .ToArray();
        }
    }
}
