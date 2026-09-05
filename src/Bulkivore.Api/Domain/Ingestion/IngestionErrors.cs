namespace Bulkivore.Api.Domain.Ingestion;

public static class IngestionErrors
{
    public static Error CannotConfirmUploadFromStatus(ImportSessionStatus status) =>
        Error.Conflict(description: $"Cannot mark as uploaded from status '{status}'.");

    public static Error CannotApplyMappingFromStatus(ImportSessionStatus status) =>
        Error.Conflict(description: $"Cannot apply mappings while session is in '{status}' status.");

    public static Error CannotCommitSessionFromStatus(ImportSessionStatus status) =>
        Error.Conflict(
            description:
            $"Cannot commit session in '{status}' status. It must be in '{ImportSessionStatus.Mapped}' status."
        );

    public static Error CannotCompleteSessionFromStatus(ImportSessionStatus status) =>
        Error.Conflict(description: $"Cannot complete session in '{status}' status.");
}
