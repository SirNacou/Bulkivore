namespace Bulkivore.Api.Domain.Imports;

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
                    ImportErrors.CannotApplyMappingFromStatus(status),
                (not ImportSessionStatus.Mapped, ImportSessionStatus.Ingesting) =>
                    ImportErrors.CannotCommitSessionFromStatus(status),
                (not ImportSessionStatus.Ingesting, ImportSessionStatus.Completed) =>
                    ImportErrors.CannotCompleteSessionFromStatus(status),
                _ => Result.Success
            };
    }
}
