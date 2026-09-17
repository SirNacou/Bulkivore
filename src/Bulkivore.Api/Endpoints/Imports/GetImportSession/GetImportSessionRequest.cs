using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.GetImportSession;

public record GetImportSessionRequest
{
    public ImportSessionId Id { get; init; }
}
