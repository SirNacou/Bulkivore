using Vogen;

namespace Bulkivore.Api.Domain.Imports;

public record RowError(
    int RowNumber,
    string Column,
    string? RawValue,
    string Reason
);
