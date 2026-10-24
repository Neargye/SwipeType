// Copyright 2018 - 2026 Daniil Goncharov <neargye@gmail.com>.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics;

namespace SwipeType.Benchmarks
{
    internal static class Program
    {
        private const int DefaultWarmupCount = 1;
        private const int DefaultIterationCount = 3;
        private const int DefaultSuggestionCount = 5;

        private static int Main(string[] args)
        {
            if (!TryParseOptions(args, out var options))
            {
                PrintUsage();
                return 2;
            }

            string baseDirectory = AppContext.BaseDirectory;
            string[] words = File.ReadAllLines(Path.Combine(baseDirectory, "EnglishDictionary.txt"));
            BenchmarkCase[] cases = LoadCases(Path.Combine(baseDirectory, "SwipePaths.tsv"));
            var suggesters = new[]
            {
                new SuggesterFactory("Geometric", dictionary => new GeometricSwipeSuggester(dictionary)),
                new SuggesterFactory("Strict", dictionary => new StrictSwipeSuggester(dictionary)),
                new SuggesterFactory("EditDistance", dictionary => new EditDistanceSwipeSuggester(dictionary))
            };

            Console.WriteLine(
                $"SwipeType benchmark | .NET {Environment.Version} | " +
                $"{words.Length:N0} words | {cases.Length} paths | top {options.SuggestionCount}");
            Console.WriteLine(
                $"Warmup: {options.WarmupCount}, measured iterations: {options.IterationCount}");
            Console.WriteLine();

            bool qualityGatePassed = true;
            foreach (SuggesterFactory suggesterFactory in suggesters)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long constructionAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                var constructionTimer = Stopwatch.StartNew();
                SwipeSuggester suggester = suggesterFactory.Create(words);
                constructionTimer.Stop();
                long constructionAllocatedBytes =
                    GC.GetAllocatedBytesForCurrentThread() - constructionAllocationStart;

                var result = MeasureQueries(suggester, cases, options);
                var quality = MeasureQuality(suggester, cases, options.SuggestionCount);

                Console.WriteLine(suggesterFactory.Name);
                Console.WriteLine(
                    $"  construct: {constructionTimer.Elapsed.TotalMilliseconds,9:F2} ms, " +
                    $"{ToMebibytes(constructionAllocatedBytes),8:F2} MiB allocated");
                Console.WriteLine(
                    $"  queries:   median {result.MedianMilliseconds,8:F2} ms, " +
                    $"p95 {result.Percentile95Milliseconds,8:F2} ms, " +
                    $"{result.QueriesPerSecond,8:F2} queries/s");
                Console.WriteLine(
                    $"  allocated: {ToKibibytes(result.AllocatedBytesPerQuery),8:F2} KiB/query");
                Console.WriteLine(
                    $"  quality:   R@1 {quality.HitsAt1}/{cases.Length}, " +
                    $"R@3 {quality.HitsAt3}/{cases.Length}, " +
                    $"R@5 {quality.HitsAt5}/{cases.Length}, " +
                    $"MRR@{quality.Depth} {quality.MeanReciprocalRank:F3}");
                if (options.SuggestionCount != 5)
                {
                    Console.WriteLine(
                        $"  gate:      recall@{options.SuggestionCount} " +
                        $"{quality.HitsAtRequestedCount}/{cases.Length}");
                }

                if (suggesterFactory.Name == "Geometric" &&
                    quality.HitsAtRequestedCount != cases.Length)
                {
                    qualityGatePassed = false;
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                qualityGatePassed
                    ? "Quality gate passed: every expected word is present in the Geometric top-N results."
                    : "Quality gate failed: at least one expected word is missing from the Geometric top-N results.");
            return qualityGatePassed ? 0 : 1;
        }

        private static BenchmarkResult MeasureQueries(SwipeSuggester suggester, IReadOnlyList<BenchmarkCase> cases, BenchmarkOptions options)
        {
            for (int i = 0; i < options.WarmupCount; ++i)
            {
                ConsumeQueries(suggester, cases, options.SuggestionCount);
            }

            var elapsedMilliseconds = new double[options.IterationCount * cases.Count];
            long allocatedBytes = 0;
            int sampleIndex = 0;
            int checksum = 0;

            for (int iteration = 0; iteration < options.IterationCount; ++iteration)
            {
                foreach (BenchmarkCase benchmarkCase in cases)
                {
                    long allocationStart = GC.GetAllocatedBytesForCurrentThread();
                    long timestamp = Stopwatch.GetTimestamp();
                    IReadOnlyList<string> suggestions = suggester
                        .GetSuggestions(benchmarkCase.SwipePath, options.SuggestionCount);
                    elapsedMilliseconds[sampleIndex++] =
                        Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
                    allocatedBytes += GC.GetAllocatedBytesForCurrentThread() - allocationStart;

                    checksum += suggestions.Count;
                    foreach (string suggestion in suggestions)
                    {
                        checksum += suggestion.Length;
                    }
                }
            }

            if (checksum == 0)
            {
                throw new InvalidOperationException("The benchmark did not produce any suggestions.");
            }

            Array.Sort(elapsedMilliseconds);
            double totalMilliseconds = elapsedMilliseconds.Sum();
            return new BenchmarkResult(
                GetPercentile(elapsedMilliseconds, 0.50),
                GetPercentile(elapsedMilliseconds, 0.95),
                elapsedMilliseconds.Length * 1000.0 / totalMilliseconds,
                (double)allocatedBytes / elapsedMilliseconds.Length);
        }

        private static void ConsumeQueries(SwipeSuggester suggester, IReadOnlyList<BenchmarkCase> cases, int suggestionCount)
        {
            int checksum = 0;
            foreach (BenchmarkCase benchmarkCase in cases)
            {
                foreach (string suggestion in suggester.GetSuggestions(benchmarkCase.SwipePath, suggestionCount))
                {
                    checksum += suggestion.Length;
                }
            }

            if (checksum == 0)
            {
                throw new InvalidOperationException("The warmup did not produce any suggestions.");
            }
        }

        private static QualityResult MeasureQuality(SwipeSuggester suggester, IReadOnlyList<BenchmarkCase> cases, int requestedSuggestionCount)
        {
            int depth = Math.Max(5, requestedSuggestionCount);
            int hitsAt1 = 0;
            int hitsAt3 = 0;
            int hitsAt5 = 0;
            int hitsAtRequestedCount = 0;
            double reciprocalRankSum = 0.0;

            foreach (BenchmarkCase benchmarkCase in cases)
            {
                IReadOnlyList<string> suggestions = suggester.GetSuggestions(benchmarkCase.SwipePath, depth);
                int rank = 0;
                for (int i = 0; i < suggestions.Count; ++i)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(suggestions[i], benchmarkCase.ExpectedWord))
                    {
                        rank = i + 1;
                        break;
                    }
                }

                if (rank == 0)
                {
                    continue;
                }

                reciprocalRankSum += 1.0 / rank;
                if (rank <= 1)
                {
                    ++hitsAt1;
                }

                if (rank <= 3)
                {
                    ++hitsAt3;
                }

                if (rank <= 5)
                {
                    ++hitsAt5;
                }

                if (rank <= requestedSuggestionCount)
                {
                    ++hitsAtRequestedCount;
                }
            }

            return new QualityResult(hitsAt1, hitsAt3, hitsAt5, hitsAtRequestedCount, reciprocalRankSum / cases.Count, depth);
        }

        private static BenchmarkCase[] LoadCases(string path)
        {
            var cases = new List<BenchmarkCase>();
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] columns = line.Split('\t');
                if (columns.Length != 2 ||
                    string.IsNullOrWhiteSpace(columns[0]) ||
                    string.IsNullOrWhiteSpace(columns[1]))
                {
                    throw new InvalidDataException($"Invalid benchmark case: '{line}'.");
                }

                cases.Add(new BenchmarkCase(columns[0], columns[1]));
            }

            return cases.Count > 0
                ? cases.ToArray()
                : throw new InvalidDataException("The benchmark corpus is empty.");
        }

        private static bool TryParseOptions(string[] args, out BenchmarkOptions options)
        {
            int warmupCount = DefaultWarmupCount;
            int iterationCount = DefaultIterationCount;
            int suggestionCount = DefaultSuggestionCount;

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "--help" || args[i] == "-h")
                {
                    options = default;
                    return false;
                }

                if (i + 1 >= args.Length ||
                    !int.TryParse(args[++i], out int value) ||
                    value < 0)
                {
                    options = default;
                    return false;
                }

                switch (args[i - 1])
                {
                    case "--warmup":
                        warmupCount = value;
                        break;
                    case "--iterations" when value > 0:
                        iterationCount = value;
                        break;
                    case "--count" when value > 0:
                        suggestionCount = value;
                        break;
                    default:
                        options = default;
                        return false;
                }
            }

            options = new BenchmarkOptions(warmupCount, iterationCount, suggestionCount);
            return true;
        }

        private static double GetPercentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            int index = Math.Max(0, Math.Min(sortedValues.Count - 1, (int)Math.Ceiling(percentile * sortedValues.Count) - 1));
            return sortedValues[index];
        }

        private static double ToKibibytes(double bytes)
        {
            return bytes / 1024.0;
        }

        private static double ToMebibytes(double bytes)
        {
            return bytes / (1024.0 * 1024.0);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "Usage: dotnet run --project SwipeType.Benchmarks -c Release -- " +
                "[--warmup N] [--iterations N] [--count N]");
        }

        private readonly record struct BenchmarkOptions(int WarmupCount, int IterationCount, int SuggestionCount);

        private readonly record struct BenchmarkCase(string ExpectedWord, string SwipePath);

        private readonly record struct BenchmarkResult(double MedianMilliseconds, double Percentile95Milliseconds,
            double QueriesPerSecond, double AllocatedBytesPerQuery);

        private readonly record struct QualityResult(int HitsAt1, int HitsAt3, int HitsAt5, int HitsAtRequestedCount,
            double MeanReciprocalRank, int Depth);

        private sealed record SuggesterFactory(string Name, Func<IEnumerable<string>, SwipeSuggester> Create);
    }
}
