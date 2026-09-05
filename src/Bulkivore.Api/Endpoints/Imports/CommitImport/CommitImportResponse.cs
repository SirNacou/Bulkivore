using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.CommitImport;

public sealed record CommitImportResponse(
    ImportSessionId Id,
    ImportSessionStatus Status,
    int TotalProcessed,
    int SuccessCount,
    int FailedCount,
    long ElapsedMilliseconds,
    IReadOnlyList<RowError> Errors
)
{
    public CommitImportResponse(ImportSession session, long elapsedMilliseconds) : this(
        session.Id,
        session.Status,
        session.ProcessedRows,
        session.SuccessRowCount,
        session.FailedRowCount,
        elapsedMilliseconds,
        session.RowErrors
    )
    {
    }
};
