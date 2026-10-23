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
    internal static class TestData
    {
        internal const string QwertyLetters = "qwertyuiopasdfghjklzxcvbnm";

        private static readonly Lazy<string[]> EnglishWords =
            new Lazy<string[]>(() => File.ReadAllLines("EnglishDictionary.txt"));

        internal static string[] EnglishDictionary => EnglishWords.Value;

        internal static SwipeSuggester CreateSuggester(string kind, IEnumerable<string> words)
        {
            return CreateSuggester(kind, words, new Dictionary<string, double>());
        }

        internal static SwipeSuggester CreateSuggester(
            string kind, IEnumerable<string> words, IReadOnlyDictionary<string, double> frequencies)
        {
            return kind switch
            {
                nameof(GeometricSwipeSuggester) => new GeometricSwipeSuggester(words, frequencies),
                nameof(StrictSwipeSuggester) => new StrictSwipeSuggester(words, frequencies),
                nameof(EditDistanceSwipeSuggester) => new EditDistanceSwipeSuggester(words, frequencies),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown suggester.")
            };
        }

        internal static string CreateString(Random random, string alphabet, int length)
        {
            var characters = new char[length];
            for (int i = 0; i < characters.Length; ++i)
            {
                characters[i] = alphabet[random.Next(alphabet.Length)];
            }

            return new string(characters);
        }

        internal static string[] CreateWords(Random random, string alphabet, int count, int minLength, int maxLength)
        {
            var words = new HashSet<string>(StringComparer.Ordinal);
            while (words.Count < count)
            {
                words.Add(CreateString(random, alphabet, random.Next(minLength, maxLength + 1)));
            }

            return words.OrderBy(word => word, StringComparer.Ordinal).ToArray();
        }

        internal static int Distance(string source, string target)
        {
            return new TextDistance.Workspace().GetDistance(source, target);
        }
    }
}
