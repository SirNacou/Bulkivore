using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.ExportImportErrors;

public record ExportImportErrorsRequest
{
    public ImportSessionId Id { get; init; }
}
