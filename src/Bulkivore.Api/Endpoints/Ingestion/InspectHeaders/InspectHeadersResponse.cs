namespace Bulkivore.Api.Endpoints.Ingestion.InspectHeaders;

public sealed record InspectHeadersResponse(IReadOnlyList<string> Headers, int TotalColumns);
