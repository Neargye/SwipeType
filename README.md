# SwipeType

SwipeType turns a keyboard swipe path (the keys a finger passed over, such as `"heqerqllo"`) into ranked dictionary words (`hello`, `hero`, ...). It is a small, deterministic `netstandard2.0` library with no dependencies, and it works in regular .NET apps and Unity.

```cs
using SwipeType;

// Bring your own word list; build the suggester once and reuse it.
var suggester = new GeometricSwipeSuggester(new[] { "world", "hero", "Hello", "help" });

IReadOnlyList<string> suggestions = suggester.GetSuggestions("heqerqllo", 3);
// Hello, hero, help
```

Reference `SwipeType/SwipeType.csproj` or the built `SwipeType.dll`. The library ships no dictionary: pass your own word list and, ideally, word frequencies.

## Choosing a suggester

| Type | Use it when |
| --- | --- |
| `GeometricSwipeSuggester` | Recommended: tolerant ranking by key geometry, nearby endpoint keys, optional frequencies. QWERTY by default, any `KeyboardLayout` on request. |
| `StrictSwipeSuggester` | Strict matching: exact first and last letters, all word letters in path order. Fast, but misses imprecise swipes. |
| `EditDistanceSwipeSuggester` | A keyboard-independent Damerau-Levenshtein fallback. Slow on large dictionaries. |

All suggesters derive from `SwipeSuggester`, so they can be swapped behind one variable. The base class cannot be derived from outside the library.

## Frequencies

Corpus frequencies decide between words with similar or identical swipes (`to`/`too`) and are the biggest quality win in practice:

```cs
var frequencies = new Dictionary<string, double> { ["hello"] = 1000, ["hero"] = 50 };
var suggester = new GeometricSwipeSuggester(words, frequencies);
```

- Values must be finite and non-negative; keys must not be blank. Keys match dictionary words case-insensitively, and entries for words outside the dictionary are ignored.
- Boosts are logarithmic and relative to the largest supplied frequency.
- `GeometricSwipeSuggester` adds the boost to the geometric score. It reorders words within a match class; an exact ordered match still beats a nearby or unmatched one.
- `StrictSwipeSuggester` and `EditDistanceSwipeSuggester` use frequencies only to order words with equal edit distance.

## Keyboard layouts

A `KeyboardLayout` is built from `KeyboardKey(character, x, y)` values. One unit of `x` is the distance between neighboring keys in a row; keys with the same `y` form a row. `KeyboardLayout.Qwerty` is the default and a starting point:

```cs
using System.Linq;
using SwipeType;

// Simplified AZERTY: only A/Q and Z/W trade places relative to QWERTY.
var azerty = new KeyboardLayout(KeyboardLayout.Qwerty.Keys.Select(key => key.Character switch
{
    'a' => new KeyboardKey('q', key.X, key.Y),
    'q' => new KeyboardKey('a', key.X, key.Y),
    'z' => new KeyboardKey('w', key.X, key.Y),
    'w' => new KeyboardKey('z', key.X, key.Y),
    _ => key
}));

SwipeSuggester suggester = new GeometricSwipeSuggester(frenchWords, azerty);
```

Layouts must be non-empty, have finite coordinates, and contain unique, non-whitespace keys after lower-casing. A layout is validated once, is immutable, and can be shared between suggesters. Layouts with up to 256 keys precompute key distances; larger layouts compute them on demand. `StrictSwipeSuggester` also accepts a layout and uses its rows for the shortest-word bound.

## Behavior

- `GetSuggestions(swipePath, maxCount)` returns at most `maxCount` words, best first. Use `int.MaxValue` for the full ranking. A smaller `maxCount` always returns a prefix of the result for a larger one.
- A null path throws `ArgumentNullException`; a negative `maxCount` throws `ArgumentOutOfRangeException`. A blank path or `maxCount` of zero returns an empty list. An edit-distance comparison needs (path length + 2) × (word length + 2) cells of memory; when a path and a candidate word would need more than `int.MaxValue` cells, `InsufficientMemoryException` is thrown. Real swipes are far below that.
- Dictionary words are trimmed and matched case-insensitively. Blank entries and case-insensitive duplicates are dropped (the first spelling is kept and returned). `Words` exposes the result.
- A swipe passes a doubled letter once, so `hello` matches `heqerqlo` as well as `heqerqllo`. For a clean swipe shared by two words (`tyuio` for `to`/`too`), the word without the repeat ranks first; a noisy path can flip that, so supply frequencies when such pairs matter.
- `GeometricSwipeSuggester` ignores path characters that are not on the layout. Words whose first or last letter is not on the layout are never suggested.
- Ranking is deterministic. `GeometricSwipeSuggester` orders by match class (an exact match means path keys at distance zero, so keys at the same position count as one), then geometric score, then Damerau-Levenshtein distance (for exact score ties), then ordinal spelling. The other suggesters order by edit distance, then frequency, then ordinal spelling.
- Suggesters copy the dictionary in the constructor and are immutable afterwards. One instance can serve concurrent `GetSuggestions` calls from several threads.

## Algorithm notes

Strict matching is based on Krishna Bharadwaj's `swype.py` gist: a word is a candidate when its letters appear in the swipe path in order, with endpoint and length filters reducing the candidate set.

`GeometricSwipeSuggester` extends that model with key geometry, nearby endpoint matching, path-to-word alignment, ordered-match classes with endpoint and length penalties, bounded top-N selection, and optional logarithmic frequency boosts.

`EditDistanceSwipeSuggester` keeps exact Damerau-Levenshtein ordering. It visits word-length buckets from the nearest length outward, stops when the length difference proves that no later word can enter the top N, and abandons each distance computation as soon as it cannot beat the current worst result.

SwipeType is not a language model. Production keyboards combine geometric ranking with frequencies and language context.

## Unity

Pick one:

- Copy `SwipeType.dll` from a Release build (`SwipeType/bin/Release/netstandard2.0/`) into `Assets/Plugins`.
- Unity 2020.2 or newer: copy the `.cs` files from `SwipeType/` into the project. They use C# 8 nullable annotations, which older Unity compilers reject.

Use the `.NET Standard` API compatibility level. The library uses no unsafe code, reflection emit, dynamic code generation, or platform-specific APIs.

## Building, benchmarks, and the quality corpus

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project SwipeType.Example --configuration Release
dotnet run --project SwipeType.Benchmarks --configuration Release -- --warmup 3 --iterations 8 --count 5
```

The benchmark runner reports construction time, latency, throughput, allocations, Recall@1/3/5, and mean reciprocal rank for the `Geometric`, `Strict`, and `EditDistance` suggesters. It uses `data/EnglishDictionary.txt` and `data/SwipePaths.tsv` and fails if an expected word is missing from the geometric top N. Compare timings only on the same machine and runtime.

## License

SwipeType is licensed under the [Apache-2.0 License](https://github.com/Neargye/SwipeType/blob/master/LICENSE). Strict matching is derived from Krishna Bharadwaj's MIT-licensed `swype.py`; its copyright and license text are in [NOTICE](https://github.com/Neargye/SwipeType/blob/master/NOTICE). The repository's test dictionary is third-party data with unresolved redistribution terms, used only by the tests, the example, and the benchmarks; see [data/README.md](https://github.com/Neargye/SwipeType/blob/master/data/README.md).
