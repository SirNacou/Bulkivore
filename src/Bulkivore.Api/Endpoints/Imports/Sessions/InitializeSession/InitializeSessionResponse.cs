namespace Bulkivore.Api.Endpoints.Imports.Sessions.InitializeSession;

public record InitializeSessionResponse(
    string SessionId,
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt
);
