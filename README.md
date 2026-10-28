# SwipeType

SwipeType turns the keys a finger swipes over into ranked word suggestions. A dependency-free `netstandard2.0` library for .NET and Unity.

## Quick start

Reference `SwipeType/SwipeType.csproj` or the built `SwipeType.dll`. Supply your own word list; no dictionary is bundled with the library.

```cs
using SwipeType;

// Create once and reuse.
var words = new[] { "world", "hero", "Hello", "help" };
var suggester = new GeometricSwipeSuggester(words);

var suggestions = suggester.GetSuggestions("heqerqllo", 3);
// Hello, hero, help
```

Results are ordered best first, with at most the requested count. Matching ignores case and preserves dictionary spelling. The geometric and strict suggesters allow one pass for a doubled letter: `heqerqlo` can match `hello`.

## Choosing a suggester

| Type | Use it when |
| --- | --- |
| `GeometricSwipeSuggester` | Recommended for swipe input. Uses key positions to tolerate imprecise paths; QWERTY by default. |
| `StrictSwipeSuggester` | Fast, strict matching: same first and last letters, word letters in path order, plus a layout-based length filter. |
| `EditDistanceSwipeSuggester` | Matches by edit distance without a keyboard layout. Slow on large dictionaries. |

Instances can be reused across threads.

## Optional settings

**Word frequencies** help rank words with similar swipes:

```cs
using System.Collections.Generic;

var frequencies = new Dictionary<string, double> { ["hello"] = 1000, ["hero"] = 50 };
var suggester = new GeometricSwipeSuggester(words, frequencies);
```

Frequency keys ignore case; values must be finite and non-negative.

**Custom layouts:** pass `new KeyboardLayout(keys)` as the second constructor argument to `GeometricSwipeSuggester` or `StrictSwipeSuggester`. Each key is a `KeyboardKey(character, x, y)`, with neighboring keys one unit apart and keys in the same row sharing `y`.

Use a layout that covers your dictionary's characters. The geometric suggester ignores unsupported path characters and may omit words containing them.

## Unity

- Copy `SwipeType.dll` from a Release build (`SwipeType/bin/Release/netstandard2.0/`) into `Assets/Plugins`.
- Or, with Unity 2020.2 or newer, copy the `.cs` files from `SwipeType/` into the project.

Use the `.NET Standard` API compatibility level.

## Building

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project SwipeType.Example --configuration Release
```

## License

[Apache-2.0](LICENSE). Strict matching derives from Krishna Bharadwaj's MIT-licensed `swype.py`; see [NOTICE](NOTICE). The test dictionary has separate redistribution restrictions; see [data/README.md](data/README.md).
