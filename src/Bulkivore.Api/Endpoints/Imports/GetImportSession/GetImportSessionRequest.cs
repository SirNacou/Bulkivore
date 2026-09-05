using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.GetImportSession;

public record GetImportSessionRequest
{
    public ImportSessionId Id { get; init; }
}
