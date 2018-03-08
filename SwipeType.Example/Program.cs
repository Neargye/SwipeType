using System;
using System.IO;

namespace SwipeType.Example
{
    internal class Program
    {
        private static void Main()
        {
            var swype = new SimpleSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
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
                var result = swype.GetSuggestion(s);
                for (var i = 0; i < result.Length; i++)
                {
                    var x = result[i];
                    Console.WriteLine("match " + (i + 1) + ": " + x);
                }
            }
            Console.ReadKey(true);
        }
    }
}