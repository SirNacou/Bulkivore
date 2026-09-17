using Bulkivore.Api.Domain.Imports;

namespace Bulkivore.Api.Endpoints.Imports.InitializeSession;

public record InitializeSessionResponse(
    ImportSessionId SessionId,
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt
);
