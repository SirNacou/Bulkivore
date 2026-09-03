namespace Bulkivore.Api.Endpoints.Imports.Sessions.InitializeSession;

public record InitializeSessionResponse(
    Guid SessionId,
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt
);
