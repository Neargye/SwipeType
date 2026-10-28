# Test and benchmark data

`EnglishDictionary.txt` is shared by the example, tests, and benchmarks.

The file has 109,581 non-empty entries and SHA-256 (with LF line endings) `3BB0B0D0D27EB7AE24057459660FFB5CD13399EA3257D851CFFF54293B07CA2B`. It matches the 1991 SIL `wordsEn.txt` list derived from the Public Brand Software word list, except for the missing entry `a`.

The preserved Public Brand Software notice contains redistribution conditions, so the dictionary is not covered by this repository's Apache-2.0 license. Replace it with a clearly licensed list before redistributing it.

References:

- [SIL list provenance and transformation notes](https://groups.google.com/g/borland.public.delphi.non-technical/c/teImqU0ugtc)
- [Preserved Public Brand Software licensing notice](https://www.kmoser.com/anagrams/)

`SwipePaths.tsv` is a tab-separated regression corpus:

```text
expected_word<TAB>swipe_path
```

The eight rows are smoke tests, not evidence of accuracy on real user input.
