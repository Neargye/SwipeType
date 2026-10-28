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

using System.Diagnostics;

namespace SwipeType.Example
{
    internal static class Program
    {
        private static readonly string[] SwipePaths =
        {
            "heqerqllo",
            "heqerqlo",
            "geqerqllp",
            "qwertyuihgfcvbnjk",
            "wertyuioiuytrtghjklkjhgfd",
            "dfghjioijhgvcftyuioiuytr",
            "aserfcvghjiuytedcftyuytre",
            "asdfgrtyuijhvcvghuiklkjuytyuytre",
            "mjuytfdsdftyuiuhgvc",
            "vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty"
        };

        private static void Main()
        {
            string[] words = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "EnglishDictionary.txt"));

            // Recommended setup: build one suggester at startup, reuse it for every swipe, and ask for top-N only.
            // Real applications pass frequencies from a corpus; these few values only illustrate the effect.
            var frequencies = new Dictionary<string, double>
            {
                ["hello"] = 10000.0,
                ["quick"] = 9000.0,
                ["world"] = 9000.0,
                ["doctor"] = 8000.0,
                ["music"] = 8000.0,
                ["architecture"] = 5000.0,
                ["agriculture"] = 5000.0,
                ["vocabulary"] = 4000.0

            };
            PrintSuggestions("Geometric (QWERTY) with frequencies (recommended)", new GeometricSwipeSuggester(words, frequencies));
            PrintSuggestions("Strict ordered path match", new StrictSwipeSuggester(words));
            PrintSuggestions("Edit-distance fallback", new EditDistanceSwipeSuggester(words));
        }

        private static void PrintSuggestions(string title, SwipeSuggester suggester)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            Console.WriteLine(new string('=', title.Length));

            foreach (string swipePath in SwipePaths)
            {
                long timestamp = Stopwatch.GetTimestamp();
                IReadOnlyList<string> suggestions = suggester.GetSuggestions(swipePath, 5);
                TimeSpan elapsed = Stopwatch.GetElapsedTime(timestamp);

                Console.WriteLine($"{swipePath} ({elapsed.TotalMilliseconds:F1} ms): {string.Join(", ", suggestions)}");
            }
        }
    }
}
