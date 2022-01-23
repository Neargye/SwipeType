# SwipeType - Implementing same algorithm "swype keyboard" for .NET and Unity

[![Build status](https://ci.appveyor.com/api/projects/status/o8nxa5oeqp9y95d6/branch/master?svg=true)](https://ci.appveyor.com/project/Neargye/swipetype/branch/master)
[![License](https://img.shields.io/github/license/Neargye/SwipeType.svg)](LICENSE)

## Example

```cs
var swype = new MatchSwipeType(File.ReadAllLines("wordlist.txt")); // File with a list of words
string testCases = "heqerqllo";

foreach (var x in swype.GetSuggestion(testCases, 2))
{
    Console.WriteLine(x);
    //Output:
    //hello
    //hero
}
```
