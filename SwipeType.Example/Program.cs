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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SwipeType.Example
{
    internal static class Program
    {
        private static void Main()
        {
            string[] words = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "EnglishDictionary.txt"));

            SampleUsingSwipeType("Strict path match", new MatchSwipeType(words));
            SampleUsingSwipeType("QWERTY geometry ranking with sample frequencies", new QwertySwipeType(words, CreateSampleFrequencies()));
            SampleUsingSwipeType("Edit-distance fallback", new DistanceSwipeType(words));
            SampleUsingFrequencies();
        }

        private static void SampleUsingSwipeType(string title, SwipeType swipeType)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            Console.WriteLine(new string('=', title.Length));

            var stopwatch = new Stopwatch();
            string[] testCases =
            {
                "heqerqllo",
                "geqerqllp",
                "qwertyuihgfcvbnjk",
                "wertyuioiuytrtghjklkjhgfd",
                "dfghjioijhgvcftyuioiuytr",
                "aserfcvghjiuytedcftyuytre",
                "asdfgrtyuijhvcvghuiklkjuytyuytre",
                "mjuytfdsdftyuiuhgvc",
                "vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty"
            };

            foreach (var s in testCases)
            {
                Console.WriteLine();
                Console.WriteLine($"Swipe path: {s}");

                stopwatch.Start();
                var result = swipeType.GetSuggestion(s, 10).ToArray();
                stopwatch.Stop();
                Console.WriteLine($"Match time: {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Reset();

                for (int i = 0; i < result.Length; ++i)
                {
                    Console.WriteLine($"{i + 1,2}. {result[i]}");
                }
            }
        }

        private static IReadOnlyDictionary<string, double> CreateSampleFrequencies()
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["hello"] = 10000.0,
                ["quick"] = 9000.0,
                ["world"] = 9000.0,
                ["doctor"] = 8000.0,
                ["architecture"] = 5000.0,
                ["agriculture"] = 5000.0,
                ["music"] = 8000.0,
                ["vocabulary"] = 4000.0
            };
        }

        private static void SampleUsingFrequencies()
        {
            Console.WriteLine();
            Console.WriteLine("Frequency-aware QWERTY ranking");
            Console.WriteLine("===============================");

            var words = new[] { "bello", "hello", "hero" };
            var frequencies = new Dictionary<string, double>
            {
                ["hello"] = 1000.0,
                ["hero"] = 50.0
            };
            SwipeType swipeType = new QwertySwipeType(words, frequencies);

            foreach (var word in swipeType.GetSuggestion("gello", 3))
            {
                Console.WriteLine(word);
            }
        }
    }
}
