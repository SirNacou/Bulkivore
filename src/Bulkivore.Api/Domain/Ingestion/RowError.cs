using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

public record RowError(
    int RowNumber,
    string Column,
    string? RawValue,
    string Reason
);
