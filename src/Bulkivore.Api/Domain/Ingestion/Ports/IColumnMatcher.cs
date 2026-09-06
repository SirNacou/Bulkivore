using Bulkivore.Api.Domain.Schema;

namespace Bulkivore.Api.Domain.Ingestion.Ports;

public sealed record ColumnMatch(
    string SourceHeader,
    string TargetColumn,
    double Confidence,
    bool IsAutoMatched
);

public interface IColumnMatcher
{
    IReadOnlyList<ColumnMatch> Match(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<ColumnMetadata> targetColumns,
        double confidenceThreshold = 0.80
    );
}
