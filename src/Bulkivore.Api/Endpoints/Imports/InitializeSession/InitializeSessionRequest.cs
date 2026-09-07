namespace Bulkivore.Api.Endpoints.Imports.InitializeSession;

public record InitializeSessionRequest
{
    public required string TargetTable { get; init; }
    public required string FileName { get; init; }
    public string? TenantId { get; init; } = null;
}
