namespace Bulkivore.Api.Domain.Ingestion;

public enum ImportSessionStatus
{
    Initialized, // Presigned URL generated, waiting on S3
    Uploaded,
    Mapped, // Column mappings confirmed by user
    Ingesting, // Binary COPY streaming into PostgreSQL
    Completed, // Successfully imported all valid rows
    Failed // Unrecoverable pipeline error
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
                (ImportSessionStatus.Initialized, not ImportSessionStatus.Uploaded) =>
                    IngestionErrors.CannotConfirmUploadFromStatus(status),
                (ImportSessionStatus.Uploaded, not ImportSessionStatus.Mapped) =>
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
