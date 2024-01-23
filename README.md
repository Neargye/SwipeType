# SwipeType - Implementing same algorithm "swype keyboard" for .NET and Unity

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

## Licensed under the [Apache-2.0 License](LICENSE)
