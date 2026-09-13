using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.ListImports;

public record ListImportsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ImportSummaryDto> Items
);

public record ImportSummaryDto(
    ImportSessionId Id,
    ImportSessionStatus Status,
    string TargetTable,
    string FileName,
    int ProcessedRows,
    int SuccessRowCount,
    int FailedRowCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
)
{
    public static ImportSummaryDto FromSession(ImportSession session) =>
        new(
            session.Id,
            session.Status,
            session.TargetTable,
            session.File.Name,
            session.ProcessedRows,
            session.SuccessRowCount,
            session.FailedRowCount,
            session.CreatedAt,
            session.CompletedAt);
};
