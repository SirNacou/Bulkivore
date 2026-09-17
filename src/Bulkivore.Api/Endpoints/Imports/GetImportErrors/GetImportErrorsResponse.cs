using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.GetImportErrors;

public record GetImportErrorsResponse(
    ImportSessionId Id,
    int TotalErrors,
    int Page,
    int PageSize,
    IReadOnlyList<RowError> Errors
);
