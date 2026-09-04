using Bulkivore.Api.Domain.Schema;

namespace Bulkivore.Api.Endpoints.Ingestion.Services;

public sealed class FuzzyColumnMatcher
{
    private const double DefaultThreshold = 0.75;

    /// <summary>
    /// Matches incoming file headers to database table columns based on normalized exact matches and Levenshtein similarity.
    /// Returns a dictionary of [SourceHeader -> TargetColumnName].
    /// </summary>
    public IReadOnlyDictionary<string, string> AutoMatch(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<ColumnMetadata> targetColumns,
        double similarityThreshold = DefaultThreshold)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var remainingTargets = targetColumns.ToList();

        // Pass 1: Exact and Normalized Matches (ignoring casing, underscores, spaces, hyphens)
        foreach (var header in sourceHeaders)
        {
            var normalizedHeader = Normalize(header);

            ColumnMetadata exactMatch = remainingTargets.FirstOrDefault(
                t =>
                    string.Equals(t.Name, header, StringComparison.OrdinalIgnoreCase)
                    || Normalize(t.Name) == normalizedHeader,
                ColumnMetadata.Empty
            );

            if (exactMatch != ColumnMetadata.Empty)
            {
                mappings[header] = exactMatch.Name;
                remainingTargets.Remove(exactMatch);
            }
        }

        // Pass 2: Fuzzy Matching using Levenshtein Distance for remaining unmatched columns
        foreach (var header in sourceHeaders)
        {
            if (mappings.ContainsKey(header) || remainingTargets.Count == 0)
            {
                continue;
            }

            var normalizedHeader = Normalize(header);
            ColumnMetadata bestTarget = ColumnMetadata.Empty;
            var bestScore = 0.0;

            foreach (var target in remainingTargets)
            {
                var normalizedTarget = Normalize(target.Name);
                var similarity = CalculateSimilarity(normalizedHeader, normalizedTarget);

                if (similarity > bestScore && similarity >= similarityThreshold)
                {
                    bestScore = similarity;
                    bestTarget = target;
                }
            }

            if (bestTarget != ColumnMetadata.Empty)
            {
                mappings[header] = bestTarget.Name;
                remainingTargets.Remove(bestTarget);
            }
        }

        return mappings;
    }

    private static string Normalize(string input) =>
        input
            .Trim()
            .ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty);

    private static double CalculateSimilarity(string source, string target)
    {
        if (source == target) return 1.0;
        if (source.Length == 0 || target.Length == 0) return 0.0;

        var distance = LevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }
}
