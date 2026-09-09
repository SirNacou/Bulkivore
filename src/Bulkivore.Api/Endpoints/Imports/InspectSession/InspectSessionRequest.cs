using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public sealed class InspectSessionRequest
{
    public ImportSessionId Id { get; init; }
}
