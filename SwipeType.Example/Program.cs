// Copyright 2018 Terik23.
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
using System.IO;

namespace SwipeType.Example
{
    internal class Program
    {
        private static void Main()
        {
            SwipeType simpleSwipeType = new SimpleSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
            string[] testCases =
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

            foreach (var s in testCases)
            {
                Console.WriteLine("#===============================#");
                Console.WriteLine($"Raw string: {s}");

                var result = simpleSwipeType.GetSuggestion(s);

                for (var i = 0; i < result.Length; i++)
                {
                    var x = result[i];
                    Console.WriteLine($"match {i + 1}: {x}");
                }
            }
            Console.ReadKey(true);

            SwipeType distanceSwipeType = new DistanceSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
            foreach (var s in testCases)
            {
                Console.WriteLine("#===============================#");
                Console.WriteLine($"Raw string: {s}");

                var result = distanceSwipeType.GetSuggestion(s);

                for (var i = 0; i < result.Length; i++)
                {
                    var x = result[i];
                    Console.WriteLine($"match {i + 1}: {x}");
                }
            }
            Console.ReadKey(true);
        }
    }
}