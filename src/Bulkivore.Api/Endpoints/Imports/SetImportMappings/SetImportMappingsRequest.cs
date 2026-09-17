using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.SetImportMappings;

public sealed record SetImportMappingsRequest
{
    public required ImportSessionId Id { get; init; }
    public List<ColumnMapping> Mappings { get; init; } = [];
}
