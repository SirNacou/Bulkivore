namespace Bulkivore.Api.Endpoints.Imports.InitializeSession;

public record InitializeSessionRequest(
    string TargetTable,
    string FileName,
    string? TenantId
);
