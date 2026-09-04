namespace Bulkivore.Api.Domain.Ingestion;

public enum ImportSessionStatus
{
    Initialized = 1, // Presigned URL generated, waiting on S3
    Inspected = 2, // Headers discovered & previewed
    Mapped = 3, // Column mappings confirmed by user
    Ingesting = 4, // Binary COPY streaming into PostgreSQL
    Completed = 5, // Successfully imported all valid rows
    Failed = 6 // Unrecoverable pipeline error
}

public static class ImportSessionStatusExtensions
{
    private static Error GetError(ImportSessionStatus status, ImportSessionStatus nextStatus) =>
        Error.Conflict(description: $"Cannot transition from status '{status}' to status '{nextStatus}'.");

    extension(ImportSessionStatus status)
    {
        public ErrorOr<Success> CanTransitionTo(ImportSessionStatus nextStatus) =>
            (status, nextStatus) switch
            {
                (ImportSessionStatus.Initialized, not ImportSessionStatus.Inspected) =>
                    IngestionErrors.CannotMarkAsInspectedFromStatus(status),
                (ImportSessionStatus.Inspected, not ImportSessionStatus.Mapped) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.Mapped, not ImportSessionStatus.Ingesting) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.Ingesting, not ImportSessionStatus.Completed) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.Ingesting, not ImportSessionStatus.Failed) =>
                    GetError(status, nextStatus),
                _ => Result.Success
            };
    }
}
