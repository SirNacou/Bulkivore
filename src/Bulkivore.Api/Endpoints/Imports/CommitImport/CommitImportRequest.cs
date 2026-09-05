using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.CommitImport;

public sealed record CommitImportRequest
{
    public ImportSessionId Id { get; init; }
}
