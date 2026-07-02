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
    public class SimpleSwipeTypeTests
    {
        [TestMethod]
        public void ConstructorThrowsForNullDictionary()
        {
            AssertThrowsArgumentNullException(() => new MatchSwipeType(null!));
        }

        [TestMethod]
        public void ConstructorFiltersBlankWordsAndTrimsDictionary()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(new[] { "", "  ", " Hello " });

            CollectionAssert.AreEqual(new[] { "Hello" }, swipeType.Words.ToArray());
        }

        [TestMethod]
        public void ConstructorCopiesDictionary()
        {
            var words = new[] { "hello", "hero" };
            SwipeType.SwipeType swipeType = new MatchSwipeType(words);

            words[0] = "zero";

            CollectionAssert.AreEqual(new[] { "hello", "hero" }, swipeType.Words.ToArray());
        }

        [TestMethod]
        public void GetSuggestionMatchesCaseInsensitivelyAndKeepsOriginalWords()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(new[] { "Hello", "hero", "help", "world" });

            var suggestions = swipeType.GetSuggestion("HEQERQLLO", 2).ToArray();

            CollectionAssert.AreEqual(new[] { "Hello", "hero" }, suggestions);
        }

        [TestMethod]
        public void GetSuggestionWithZeroCountReturnsNoResults()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(new[] { "hello", "hero" });

            Assert.AreEqual(0, swipeType.GetSuggestion("heqerqllo", 0).Count());
        }

        [TestMethod]
        public void GetSuggestionWithNegativeCountThrows()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(new[] { "hello", "hero" });

            AssertThrowsArgumentOutOfRangeException(() => swipeType.GetSuggestion("heqerqllo", -1).ToArray());
        }

        [TestMethod]
        public void GetSuggestionWithBlankInputReturnsNoResults()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(new[] { "hello", "hero" });

            Assert.AreEqual(0, swipeType.GetSuggestion("   ").Count());
        }

        [TestMethod]
        public void GetSuggestionFindsExpectedWordsFromDictionaryFile()
        {
            SwipeType.SwipeType swipeType = new MatchSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
            var suggestions = swipeType.GetSuggestion("heqerqllo", 3).ToArray();

            CollectionAssert.AreEqual(new[] { "hello", "hero", "ho" }, suggestions);
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

        private static void AssertThrowsArgumentOutOfRangeException(Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected ArgumentOutOfRangeException.");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
    }
}
