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
        [DataRow("aa", "a", 1)]
        [DataRow("aab", "ab", 1)]
        [DataRow("ca", "abc", 2)]
        [DataRow("kitten", "sitting", 3)]
        public void DistanceReturnsExpectedValues(string first, string second, int expected)
        {
            Assert.AreEqual(expected, TestData.Distance(first, second));
            Assert.AreEqual(expected, TestData.Distance(second, first));
        }

        [TestMethod]
        public void DistanceWorkspaceCanBeReusedAcrossDifferentWordLengths()
        {
            var workspace = new TextDistance.Workspace();
            var cases = new[]
            {
                new[] { "swipe", "swipe" },
                new[] { "swipe", "wipe" },
                new[] { "swipe", "sip" },
                new[] { "swipe", "switch" },
                new[] { "swipe", "pies" },
                new[] { "a", "abcdefghijk" },
                new[] { "transposition", "transportation" },
                new[] { "ca", "abc" },
                new[] { "", "abc" },
                new[] { "aab", "ab" }
            };

            foreach (string[] pair in cases)
            {
                Assert.AreEqual(
                    TestData.Distance(pair[0], pair[1]),
                    workspace.GetDistance(pair[0], pair[1]),
                    $"Workspace reuse changed the distance for '{pair[0]}' and '{pair[1]}'.");
            }
        }

        [TestMethod]
        public void DistanceWorkspaceRejectsDimensionsThatOverflowItsStorage()
        {
            var workspace = new TextDistance.Workspace();

            Assert.ThrowsExactly<InsufficientMemoryException>(
                () => workspace.GetDistance(new string('a', 50000), new string('b', 50000)));
            Assert.AreEqual(0, workspace.GetDistance(new string('a', 50000), new string('a', 50000)));
        }

        [TestMethod]
        public void DistanceRespectsLengthLowerBoundAndSymmetry()
        {
            const string alphabet = "aabc";
            var random = new Random(6101);

            for (int iteration = 0; iteration < 500; ++iteration)
            {
                string first = TestData.CreateString(random, alphabet, random.Next(0, 20));
                string second = TestData.CreateString(random, alphabet, random.Next(0, 20));
                int distance = TestData.Distance(first, second);

                Assert.IsTrue(
                    distance >= Math.Abs(first.Length - second.Length),
                    $"Distance {distance} is below the length lower bound for '{first}' and '{second}'.");
                Assert.AreEqual(
                    distance,
                    TestData.Distance(second, first),
                    $"Distance is not symmetric for '{first}' and '{second}'.");
            }
        }

        [TestMethod]
        public void DistanceMatchesIndependentEditGraphOnSmallStrings()
        {
            IReadOnlyList<string> strings = CreateStrings("abc", 3);

            foreach (string first in strings)
            {
                foreach (string second in strings)
                {
                    Assert.AreEqual(
                        GetReferenceDistance(first, second, "abc"),
                        TestData.Distance(first, second),
                        $"Distance differs from the edit-graph result for '{first}' and '{second}'.");
                }
            }
        }

        [TestMethod]
        public void BoundedDistanceIsExactWithinTheBoundAndExceedsItOtherwise()
        {
            const string alphabet = "aabcé";
            var random = new Random(6102);
            var workspace = new TextDistance.Workspace();

            for (int iteration = 0; iteration < 2000; ++iteration)
            {
                string first = TestData.CreateString(random, alphabet, random.Next(0, 14));
                string second = TestData.CreateString(random, alphabet, random.Next(0, 14));
                int expected = TestData.Distance(first, second);

                for (int maxDistance = 0; maxDistance <= 14; ++maxDistance)
                {
                    int bounded = workspace.GetDistance(first, second, maxDistance);
                    if (expected <= maxDistance)
                    {
                        Assert.AreEqual(expected, bounded, $"'{first}' and '{second}' with bound {maxDistance}.");
                    }
                    else
                    {
                        Assert.IsTrue(bounded > maxDistance, $"'{first}' and '{second}' with bound {maxDistance}.");
                    }
                }
            }
        }

        private static IReadOnlyList<string> CreateStrings(string alphabet, int maximumLength)
        {
            var result = new List<string> { "" };
            var previousLength = new List<string> { "" };

            for (int length = 1; length <= maximumLength; ++length)
            {
                var currentLength = new List<string>();
                foreach (string prefix in previousLength)
                {
                    foreach (char value in alphabet)
                    {
                        currentLength.Add(prefix + value);
                    }
                }

                result.AddRange(currentLength);
                previousLength = currentLength;
            }

            return result;
        }

        private static int GetReferenceDistance(string source, string target, string alphabet)
        {
            int maximumLength = Math.Max(source.Length, target.Length) + 1;
            var pending = new Queue<(string Value, int Distance)>();
            var visited = new HashSet<string>(StringComparer.Ordinal) { source };
            pending.Enqueue((source, 0));

            while (pending.Count != 0)
            {
                (string current, int distance) = pending.Dequeue();
                if (current == target)
                {
                    return distance;
                }

                for (int index = 0; index < current.Length; ++index)
                {
                    Enqueue(current.Remove(index, 1), distance + 1, visited, pending);

                    foreach (char value in alphabet)
                    {
                        if (value != current[index])
                        {
                            Enqueue(
                                current.Remove(index, 1).Insert(index, value.ToString()),
                                distance + 1,
                                visited,
                                pending);
                        }
                    }

                    if (index + 1 < current.Length && current[index] != current[index + 1])
                    {
                        char[] transposed = current.ToCharArray();
                        (transposed[index], transposed[index + 1]) =
                            (transposed[index + 1], transposed[index]);
                        Enqueue(new string(transposed), distance + 1, visited, pending);
                    }
                }

                if (current.Length < maximumLength)
                {
                    for (int index = 0; index <= current.Length; ++index)
                    {
                        foreach (char value in alphabet)
                        {
                            Enqueue(
                                current.Insert(index, value.ToString()),
                                distance + 1,
                                visited,
                                pending);
                        }
                    }
                }
            }

            throw new AssertFailedException($"No edit path found from '{source}' to '{target}'.");
        }

        private static void Enqueue(
            string value,
            int distance,
            ISet<string> visited,
            Queue<(string Value, int Distance)> pending)
        {
            if (visited.Add(value))
            {
                pending.Enqueue((value, distance));
            }
        }
    }
}
