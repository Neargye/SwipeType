# SwipeType

SwipeType is a small .NET library for turning swipe paths into word suggestions. It works in regular .NET projects and in Unity projects that support `netstandard2.0`.

Use:

- `QwertySwipeType` for tolerant English QWERTY swipe ranking.
- `MatchSwipeType` for strict ordered path matching.
- `DistanceSwipeType` as a simple edit-distance fallback.

## Example

```cs
using System.Collections.Generic;
using System.IO;
using SwipeType;

var swipeType = new QwertySwipeType(File.ReadAllLines("wordlist.txt"));
string swipePath = "heqerqllo";

foreach (string suggestion in swipeType.GetSuggestion(swipePath, 2))
{
    Console.WriteLine(suggestion);
    // Output:
    // hello
    // hero
}
```

If you have word frequencies, pass them to improve ranking:

```cs
var frequencies = new Dictionary<string, double>
{
    ["hello"] = 1000,
    ["hero"] = 50
};
var swipeType = new QwertySwipeType(File.ReadAllLines("wordlist.txt"), frequencies);
```

Frequencies are especially useful with large dictionaries and noisy swipe paths, where several uncommon words can match the same keyboard geometry.

## Algorithm notes

The original idea is based on Krishna Bharadwaj's `swype.py` gist: a word is a candidate when its letters appear in the swipe path in order, with endpoint and minimum-length filters to keep the candidate set small.

`MatchSwipeType` keeps that strict path-matching behavior. `QwertySwipeType` adds QWERTY keyboard geometry, nearby endpoint matching, dynamic path-to-word alignment, bounded top-N ranking, and optional word-frequency boosts.

This is a deterministic swipe-ranking library, not a full keyboard language model. With large dictionaries and noisy paths, several rare words can be geometrically valid matches, so passing word frequencies is recommended for production-quality ranking.

## Unity

SwipeType targets `netstandard2.0`, so it can be used as a managed plug-in in Unity projects that use the .NET Standard API compatibility level.

Recommended setup:

1. Build `SwipeType` in `Release` configuration.
2. Copy `SwipeType/bin/Release/netstandard2.0/SwipeType.dll` into your Unity project's `Assets/Plugins` folder.
3. Keep Unity's Player Settings > Other Settings > Api Compatibility Level on `.NET Standard` unless your project has a specific reason to use `.NET Framework`.

You can also copy the source files from `SwipeType/` directly into a Unity project. The library doesn't use dynamic code generation, reflection emit, unsafe code, or platform-specific APIs.

## License

SwipeType is licensed under the [Apache-2.0 License](LICENSE).
