using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.SetImportMappings;

public sealed record SetImportMappingsResponse(
    ImportSessionId Id,
    ImportSessionStatus Status,
    int MappedColumnCount
);
