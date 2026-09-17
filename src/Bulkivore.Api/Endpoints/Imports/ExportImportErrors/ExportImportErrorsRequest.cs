using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.ExportImportErrors;

public record ExportImportErrorsRequest
{
    public ImportSessionId Id { get; init; }
}
