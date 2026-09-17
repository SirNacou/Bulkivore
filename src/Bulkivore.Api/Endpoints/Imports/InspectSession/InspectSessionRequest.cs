using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public sealed class InspectSessionRequest
{
    public ImportSessionId Id { get; init; }
}
