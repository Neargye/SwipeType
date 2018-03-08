## SwipeType - implement the same algorithm "keyboard Swype" for .NET and Unity.

Example

```cs
var swype = new SimpleSwipeType(File.ReadAllLines("wordlist.txt")); // File with a list of words
string testCases = "heqerqllo";

foreach (var x in swype.GetSuggestion(testCases))
{
    Console.WriteLine(x);
    //Output:
    //hello
    //hero
    //ho
}
```

In the process of development VR games, it was necessary to implement a keyboard, and decided to implement its class for the Swype keyboard.