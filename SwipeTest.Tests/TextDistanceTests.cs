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
    public class TextDistanceTests
    {
        [TestMethod]
        [DataRow("", "", 0)]
        [DataRow("", "abc", 3)]
        [DataRow("abc", "", 3)]
        [DataRow("abc", "abc", 0)]
        [DataRow("abc", "ab", 1)]
        [DataRow("abc", "axc", 1)]
        [DataRow("abc", "acb", 1)]
        [DataRow("kitten", "sitting", 3)]
        public void GetDamerauLevenshteinDistanceReturnsExpectedDistance(string first, string second, int expected)
        {
            Assert.AreEqual(expected, first.GetDamerauLevenshteinDistance(second));
            Assert.AreEqual(expected, second.GetDamerauLevenshteinDistance(first));
        }
    }
}
