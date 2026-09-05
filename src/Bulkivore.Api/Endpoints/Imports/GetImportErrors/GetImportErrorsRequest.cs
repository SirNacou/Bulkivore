using Bulkivore.Api.Domain.Ingestion;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports.GetImportErrors;

public record GetImportErrorsRequest
{
    [RouteParam]
    public ImportSessionId Id { get; init; }

    [QueryParam]
    public int Page { get; init; } = 1;

    [QueryParam]
    public int PageSize { get; init; } = 50;
}
