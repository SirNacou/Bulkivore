using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.CommitImport;

public sealed record CommitImportRequest
{
    public ImportSessionId Id { get; init; }
}
