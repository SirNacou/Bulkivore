namespace Bulkivore.Api.Domain.Ingestion;

public static class IngestionErrors
{
    public static Error CannotMarkAsUploadedFromStatus(ImportSessionStatus status) =>
        Error.Conflict(description: $"Cannot mark as uploaded from status '{status}'.");
}
