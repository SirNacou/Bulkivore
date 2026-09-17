using Bulkivore.Api.Domain.Imports;
using Bulkivore.Api.Domain.Imports.Ports;
using Bulkivore.Api.Domain.Schema;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public sealed record InspectSessionResponse(
    ImportSessionId SessionId,
    ImportSessionStatus Status,
    IReadOnlyList<string> Headers,
    IReadOnlyList<ColumnMatch> SuggestedMappings,
    IReadOnlyList<ColumnMetadata> TargetColumns,
    IReadOnlyList<Dictionary<string, object>> PreviewRows
);
