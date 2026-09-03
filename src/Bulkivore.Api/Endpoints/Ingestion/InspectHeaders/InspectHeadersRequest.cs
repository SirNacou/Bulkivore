namespace Bulkivore.Api.Endpoints.Ingestion.InspectHeaders;

public sealed class InspectHeadersRequest
{
    public IFormFile File { get; set; } = null!;
}
