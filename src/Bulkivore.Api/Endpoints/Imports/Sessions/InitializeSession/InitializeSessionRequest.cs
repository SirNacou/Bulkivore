namespace Bulkivore.Api.Endpoints.Imports.Sessions.InitializeSession;

public record InitializeSessionRequest(
    string TargetTable,
    string FileName,
    string? TenantId
);
