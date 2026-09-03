namespace Bulkivore.Api.Endpoints.Imports.Sessions.InitializeSession;

public class InitializeSessionRequest
{
    public string TargetTable { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? TenantId { get; set; }
}
