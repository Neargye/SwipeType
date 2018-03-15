using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwipeType;

namespace SwipeTest.Tests
{
    [TestClass]
    public class SimpleSwipeTypeTests
    {
        [TestMethod]
        public void SimpleSwipeTypeTest()
        {
            try
            {
                var swype = new SimpleSwipeType(File.ReadAllLines("EnglishDictionary.txt"));
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
                var swype = new SimpleSwipeType(File.ReadAllLines("FullEnglishDictionary.txt"));
                var testing = new Dictionary<string, List<string>>
                {
                    ["heqerqllo"] = new List<string>
                    {
                        "hello",
                        "hero",
                        "ho"
                    },
                    ["qwertyuihgfcvbnjk"] = new List<string>
                    {
                        "quick"
                    },
                    ["wertyuioiuytrtghjklkjhgfd"] = new List<string>
                    {
                        "wed",
                        "weird",
                        "weld",
                        "wild",
                        "wold",
                        "word",
                        "world",
                        "would"
                    },
                    ["dfghjioijhgvcftyuioiuytr"] = new List<string>
                    {
                        "doctor",
                        "door",
                        "dour"
                    },
                    ["aserfcvghjiuytedcftyuytre"] = new List<string>
                    {
                        "architecture",
                        "architecure"
                    },
                    ["asdfgrtyuijhvcvghuiklkjuytyuytre"] = new List<string>
                    {
                        "adjure",
                        "agriculture",
                        "article",
                        "astute"
                    },
                    ["mjuytfdsdftyuiuhgvc"] = new List<string>
                    {
                        "music",
                        "mystic"
                    },
                    ["vghjioiuhgvcxsasdvbhuiklkjhgfdsaserty"] = new List<string>
                    {
                        "vocabulary"
                    }
                };

                foreach (var s in testing)
                {
                    foreach (var x in swype.GetSuggestion(s.Key))
                        if (!s.Value.Remove(x))
                            Console.WriteLine($"New match: {x}");
                }

                foreach (var s in testing)
                    if (s.Value.Count > 0)
                        Assert.Fail("GetSuggestionTest fail with new match");
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetSuggestionTest fail with exception: {ex}");
            }
        }
    }
}