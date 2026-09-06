using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Domain.Schema;
using Raffinert.FuzzySharp;
using Raffinert.FuzzySharp.PreProcess;
using Raffinert.FuzzySharp.SimilarityRatio.Scorer.Composite;

namespace Bulkivore.Api.Infrastructure.Services;

public class FuzzyColumnMatcher : IColumnMatcher
{
    public IReadOnlyList<ColumnMatch> Match(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<ColumnMetadata> targetColumns,
        double confidenceThreshold = 0.8)
    {
        if (sourceHeaders.Count == 0 || targetColumns.Count == 0)
            return [];

        var thresholdScore = (int)confidenceThreshold * 100;
        var unassignedTargets = targetColumns.Select(c => c.Name).ToList();
        var matches = new List<ColumnMatch>(sourceHeaders.Count);

        foreach (var header in sourceHeaders)
        {
            if (string.IsNullOrWhiteSpace(header))
                continue;

            if (unassignedTargets.Count == 0)
            {
                matches.Add(new ColumnMatch(header, string.Empty, 0, IsAutoMatched: false));
                continue;
            }

            var result = Process.ExtractOne(
                header,
                unassignedTargets,
                processor: StringPreprocessor.Full,
                scorer: new WeightedRatioScorer(),
                cutoff: thresholdScore
            );

            if (result is not null)
            {
                matches.Add(
                    new ColumnMatch(header, result.Value, Math.Round(result.Score / 100.0, 2), IsAutoMatched: true)
                );

                unassignedTargets.Remove(result.Value);
            }
            else
            {
                matches.Add(new ColumnMatch(header, string.Empty, 0, IsAutoMatched: false));
            }
        }

        return matches;
    }
}
