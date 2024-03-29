// Copyright 2018 - 2024 Daniil Goncharov <neargye@gmail.com>.
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
        public void SimpleSwipeTypeTest()
        {
            try
            {
                SwipeType.SwipeType swipeType = new DistanceSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
            }
            catch (OutOfMemoryException e)
            {
                Assert.Fail($"GetSuggestionTest fail with OutOfMemoryException: {e}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetSuggestionTest fail with Exception: {ex}");
            }
        }

        [TestMethod]
        public void GetSuggestionTest()
        {
            try
            {
                SwipeType.SwipeType swipeType = new DistanceSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
                string[] testing =
                {
                    "heqerqllo",
                    "qwertyuihgfcvbnjk",
                    "wertyuioiuytrtghjklkjhgfd",
                    "dfghjioijhgvcftyuioiuytr",
                    "aserfcvghjiuytedcftyuytre",
                    "asdfgrtyuijhvcvghuiklkjuytyuytre",
                    "mjuytfdsdftyuiuhgvc",
                    "vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty"
                };

                foreach (var s in testing)
                {
                    var x = swipeType.GetSuggestion(s);
                    if (x.Count() <= 0)
                    {
                        Assert.Fail("GetSuggestionTest fail with no match");
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetSuggestionTest fail with exception: {ex}");
            }
        }

        [TestMethod]
        public void GetSuggestionCountTest()
        {
            try
            {
                SwipeType.SwipeType swipeType = new DistanceSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
                string[] testing =
                {
                    "heqerqllo",
                    "qwertyuihgfcvbnjk",
                    "wertyuioiuytrtghjklkjhgfd",
                    "dfghjioijhgvcftyuioiuytr",
                    "aserfcvghjiuytedcftyuytre",
                    "asdfgrtyuijhvcvghuiklkjuytyuytre",
                    "mjuytfdsdftyuiuhgvc",
                    "vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty"
                };

                foreach (var s in testing)
                {
                    const int count = 3;
                    var x = swipeType.GetSuggestion(s, count);
                    if (x.Count() < count)
                    {
                        Assert.Fail("GetSuggestionTest fail with no match");
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetSuggestionTest fail with exception: {ex}");
            }
        }
    }
}
