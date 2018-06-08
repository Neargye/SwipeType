# SwipeType - implement the same algorithm "keyboard Swype" for .NET and Unity

[![Build status](https://ci.appveyor.com/api/projects/status/o8nxa5oeqp9y95d6/branch/master?svg=true)](https://ci.appveyor.com/project/Neargye/swipetype/branch/master)
[![License](https://img.shields.io/github/license/Neargye/SwipeType.svg)](LICENSE)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/f45724bbfdb34ee699696c84c4c82cf3)](https://www.codacy.com/app/Neargye/SwipeType?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=Neargye/SwipeType&amp;utm_campaign=Badge_Grade)

Example

```cs
var swype = new SimpleSwipeType(File.ReadAllLines("wordlist.txt")); // File with a list of words
string testCases = "heqerqllo";

foreach (var x in swype.GetSuggestion(testCases, 2))
{
    Console.WriteLine(x);
    //Output:
    //hello
    //hero
}
```

In the process of development VR games, it was necessary to implement a keyboard, and decided to implement its class for the Swype keyboard.