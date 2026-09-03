namespace Bulkivore.Api.Domain.Ingestion;

public enum ImportSessionStatus
{
    Initialized,
    Uploaded,
    MappingConfigured,
    Committing,
    Completed,
    Failed
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
                    IngestionErrors.CannotMarkAsUploadedFromStatus(status),
                (ImportSessionStatus.Uploaded, not ImportSessionStatus.MappingConfigured) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.MappingConfigured, not ImportSessionStatus.Committing) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.Committing, not ImportSessionStatus.Completed) =>
                    GetError(status, nextStatus),
                (ImportSessionStatus.Committing, not ImportSessionStatus.Failed) =>
                    GetError(status, nextStatus),
                _ => Result.Success
            };
    }
}
