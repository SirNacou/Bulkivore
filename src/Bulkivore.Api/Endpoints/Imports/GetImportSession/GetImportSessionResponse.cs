using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.GetImportSession;

public record GetImportSessionResponse(
    ImportSessionId Id,
    ImportSessionStatus Status,
    string TargetTable,
    string FileName,
    int ProcessedRows,
    int SuccessRowCount,
    int FailedRowCount,
    string? ErrorMessage,
    IReadOnlyList<ColumnMapping> Mappings,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
)
{
    public static GetImportSessionResponse FromSession(ImportSession session) =>
        new(
            session.Id,
            session.Status,
            session.TargetTable,
            session.File.Name,
            session.ProcessedRows,
            session.SuccessRowCount,
            session.FailedRowCount,
            session.ErrorMessage,
            session.ColumnMappings,
            session.CreatedAt,
            session.CompletedAt);
};
