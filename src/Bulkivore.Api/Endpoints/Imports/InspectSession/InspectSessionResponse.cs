using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Schema;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public sealed record InspectSessionResponse(
    ImportSessionId SessionId,
    ImportSessionStatus Status,
    IReadOnlyList<string> Headers,
    IReadOnlyDictionary<string, string> SuggestedMappings,
    IReadOnlyList<ColumnMetadata> TargetColumns,
    IReadOnlyList<Dictionary<string, object>> PreviewRows
);
