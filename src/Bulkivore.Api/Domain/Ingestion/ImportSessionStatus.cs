namespace Bulkivore.Api.Domain.Ingestion;

public enum ImportSessionStatus
{
    Initialized, // Presigned URL generated, waiting on S3
    Mapped, // Column mappings confirmed by user
    Ingesting, // Binary COPY streaming into PostgreSQL
    Completed, // Successfully imported all valid rows
    Failed // Unrecoverable pipeline error
}

public static class ImportSessionStatusExtensions
{
    extension(ImportSessionStatus status)
    {
        public ErrorOr<Success> CanTransitionTo(ImportSessionStatus nextStatus) =>
            (status, nextStatus) switch
            {
                (not (ImportSessionStatus.Initialized or ImportSessionStatus.Mapped), ImportSessionStatus.Mapped) =>
                    IngestionErrors.CannotApplyMappingFromStatus(status),
                (not ImportSessionStatus.Mapped, ImportSessionStatus.Ingesting) =>
                    IngestionErrors.CannotCommitSessionFromStatus(status),
                (not ImportSessionStatus.Ingesting, ImportSessionStatus.Completed) =>
                    IngestionErrors.CannotCompleteSessionFromStatus(status),
                _ => Result.Success
            };
    }
}
