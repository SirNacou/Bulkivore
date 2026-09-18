using Bulkivore.Api.Domain.Imports.Ports;
using Bulkivore.Api.Domain.Schema;
using Raffinert.FuzzySharp;
using Raffinert.FuzzySharp.PreProcess;
using Raffinert.FuzzySharp.SimilarityRatio.Scorer.Composite;

namespace Bulkivore.Api.Infrastructure.Services;

public class FuzzyColumnMatcher : IColumnMatcher
{
    private static readonly WeightedRatioScorer Scorer = new();
    private static readonly Func<string, string> Processor = StringPreprocessor.Full;

    public IReadOnlyList<ColumnMatch> Match(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<ColumnMetadata> targetColumns,
        double confidenceThreshold = 0.8)
    {
        if (sourceHeaders.Count == 0 || targetColumns.Count == 0)
            return [];

        var cutoff = (int)Math.Round(confidenceThreshold * 100);
        var candidates = ExtractCandidates(sourceHeaders, targetColumns, cutoff);
        var matches = ResolveAssignments(candidates, sourceHeaders.Count);

        return BuildOutput(sourceHeaders, matches);
    }

    private static List<ScoredCandidate> ExtractCandidates(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<ColumnMetadata> targetColumns,
        int cutoff)
    {
        var targetNames = targetColumns.Select(c => c.Name).ToList();
        var targetIndexes = targetNames
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        var candidates = new List<ScoredCandidate>();

        for (var i = 0; i < sourceHeaders.Count; i++)
        {
            var header = sourceHeaders[i];
            if (string.IsNullOrWhiteSpace(header))
                continue;

            var normalizedHeader = Processor(header);
            var results = Process.ExtractTop(
                header,
                targetNames,
                processor: Processor,
                scorer: Scorer,
                limit: targetNames.Count,
                cutoff: cutoff);

            foreach (var result in results)
            {
                var isExact = normalizedHeader == Processor(result.Value);
                candidates.Add(
                    new ScoredCandidate(
                        header,
                        i,
                        result.Value,
                        targetIndexes[result.Value],
                        result.Score,
                        isExact));
            }
        }

        return candidates;
    }

    private static ColumnMatch?[] ResolveAssignments(List<ScoredCandidate> candidates, int totalHeaders)
    {
        // Pass 1 (Exact matches) followed by Pass 2 (highest scores, keeping header/target tie-breaks)
        var orderedCandidates = candidates
            .OrderByDescending(c => c.IsExact)
            .ThenByDescending(c => c.Score)
            .ThenBy(c => c.HeaderIndex)
            .ThenBy(c => c.TargetIndex);

        var matches = new ColumnMatch?[totalHeaders];
        var assignedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in orderedCandidates)
        {
            if (matches[c.HeaderIndex] is not null || !assignedTargets.Add(c.TargetName))
                continue;

            matches[c.HeaderIndex] = new ColumnMatch(
                c.Header,
                c.TargetName,
                Math.Round(c.Score / 100.0, 2),
                IsAutoMatched: true);
        }

        return matches;
    }

    private static List<ColumnMatch> BuildOutput(IReadOnlyList<string> sourceHeaders, ColumnMatch?[] matches)
    {
        var output = new List<ColumnMatch>(sourceHeaders.Count);

        for (var i = 0; i < sourceHeaders.Count; i++)
        {
            var header = sourceHeaders[i];
            if (string.IsNullOrWhiteSpace(header))
                continue;

            output.Add(matches[i] ?? new ColumnMatch(header, string.Empty, 0, IsAutoMatched: false));
        }

        return output;
    }

    private sealed record ScoredCandidate(
        string Header,
        int HeaderIndex,
        string TargetName,
        int TargetIndex,
        double Score,
        bool IsExact
    );
}
