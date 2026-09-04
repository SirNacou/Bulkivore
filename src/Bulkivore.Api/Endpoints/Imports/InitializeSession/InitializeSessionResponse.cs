namespace Bulkivore.Api.Endpoints.Imports.InitializeSession;

public record InitializeSessionResponse(
    Guid SessionId,
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt
);
