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
    public class DistanceSwipeTypeTests
    {
        [TestMethod]
        public void ConstructorThrowsForNullDictionary()
        {
            AssertThrowsArgumentNullException(() => new DistanceSwipeType(null!));
        }

        [TestMethod]
        public void GetSuggestionRanksClosestWordsFirst()
        {
            SwipeType.SwipeType swipeType = new DistanceSwipeType(new[] { "world", "Hello", "hero" });

            var suggestions = swipeType.GetSuggestion("hello", 3).ToArray();

            CollectionAssert.AreEqual(new[] { "Hello", "hero", "world" }, suggestions);
        }

        [TestMethod]
        public void GetSuggestionWithZeroCountReturnsNoResults()
        {
            SwipeType.SwipeType swipeType = new DistanceSwipeType(new[] { "hello", "hero" });

            Assert.AreEqual(0, swipeType.GetSuggestion("hello", 0).Count());
        }

        [TestMethod]
        public void GetSuggestionReturnsRequestedNumberOfResults()
        {
            SwipeType.SwipeType swipeType = new DistanceSwipeType(File.ReadAllLines("EnglishDictionary.txt"));

            var suggestions = swipeType.GetSuggestion("heqerqllo", 3).ToArray();

            Assert.AreEqual(3, suggestions.Length);
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
    }
}
