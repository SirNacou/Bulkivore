using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.GetImportSession;

public record GetImportSessionResponse(
    ImportSessionId Id,
    ImportSessionStatus Status,
    string TargetTable,
    string FileName,
    int ProcessedRows,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string> Mappings,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
)
{
    public GetImportSessionResponse(ImportSession session) : this(
        session.Id,
        session.Status,
        session.TargetTable,
        session.FileName.Name,
        session.ProcessedRows,
        session.ErrorMessage,
        session.ColumnMappings,
        session.CreatedAt,
        session.CompletedAt
    )
    {
    }
};
