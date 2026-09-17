using Bulkivore.Api.Domain.Imports;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports.ListImports;

public record ListImportsRequest
{
    [QueryParam]
    public int Page { get; init; } = 1;

    [QueryParam]
    public int PageSize { get; init; } = 50;

    [QueryParam]
    public ImportSessionStatus? Status { get; init; }
}
