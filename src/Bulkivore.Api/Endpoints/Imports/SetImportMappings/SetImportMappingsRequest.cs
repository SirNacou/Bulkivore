using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.SetImportMappings;

public sealed record SetImportMappingsRequest
{
    public ImportSessionId Id { get; init; }
    public Dictionary<string, string> Mappings { get; init; } = [];
}
