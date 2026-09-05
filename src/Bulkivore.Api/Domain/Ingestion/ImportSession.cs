using Bulkivore.Api.Domain.Common;
using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

[ValueObject<Guid>] public readonly partial record struct ImportSessionId;

public sealed class ImportSession : AggregateRoot<ImportSessionId>
{
    public string TenantId { get; init; }
    public string TargetTable { get; init; }
    public SourceFileName FileName { get; init; }
    public string StorageKey { get; init; }
    public ImportSessionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    private Dictionary<string, string> _columnMappings;
    public IReadOnlyDictionary<string, string> ColumnMappings => _columnMappings;
    public int ProcessedRows => SuccessRowCount + FailedRowCount;
    public int SuccessRowCount { get; private set; }
    public int FailedRowCount { get; private set; }
    public List<RowError> RowErrors { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ImportSession(string tenantId, string targetTable, SourceFileName fileName, string storageKey)
        : base(ImportSessionId.FromNewVersion7Guid())
    {
        TenantId = tenantId;
        TargetTable = targetTable;
        FileName = fileName;
        StorageKey = storageKey;
        Status = ImportSessionStatus.Initialized;
        CreatedAt = DateTimeOffset.UtcNow;
        _columnMappings = [];
    }

    public static ErrorOr<ImportSession> Initialize(
        string? tenantId,
        string targetTable,
        string fileName,
        string storageKey)
    {
        List<Error> errors = [];
        if (string.IsNullOrWhiteSpace(targetTable))
        {
            errors.Add(
                Error.Validation(
                    description: "Target table name cannot be empty.",
                    metadata:
                    new() { [nameof(targetTable)] = targetTable }
                )
            );
        }

        var errorOrFileName = SourceFileName.Create(fileName);
        if (errorOrFileName.IsError) errors.AddRange(errorOrFileName.Errors);

        if (errors.Count > 0) return errors;

        var importSession = new ImportSession(
            string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId,
            targetTable,
            errorOrFileName.Value,
            storageKey
        );

        return importSession;
    }

    public ErrorOr<Success> ConfirmUpload() =>
        Status
            .CanTransitionTo(ImportSessionStatus.Uploaded)
            .ThenDo(_ => Status = ImportSessionStatus.Uploaded);

    public ErrorOr<Success> TransitionTo(ImportSessionStatus newStatus) =>
        Status
            .CanTransitionTo(newStatus)
            .ThenDo(_ => Status = newStatus);

    public ErrorOr<Success> ApplyMappings(
        IReadOnlyDictionary<string, string> mappings,
        IReadOnlyList<string> requiredTargetColumns)
    {
        if (Status.CanTransitionTo(ImportSessionStatus.Mapped) is { IsError: true } error) return error;

        if (mappings.Count == 0) return Error.Validation(description: "At least one column mapping must be provided.");

        HashSet<string> mappedTargets = new(mappings.Values, StringComparer.OrdinalIgnoreCase);
        var missingRequired = requiredTargetColumns.Except(mappedTargets).ToList();
        if (missingRequired.Count > 0)
        {
            return Error.Validation(
                description: $"The following required columns are missing: {string.Join(", ", missingRequired)}",
                metadata: new() { [nameof(requiredTargetColumns)] = string.Join(", ", requiredTargetColumns) }
            );
        }

        _columnMappings = new Dictionary<string, string>(mappings, StringComparer.OrdinalIgnoreCase);
        Status = ImportSessionStatus.Mapped;

        return Result.Success;
    }

    public ErrorOr<Success> StartIngesting() =>
        Status
            .CanTransitionTo(ImportSessionStatus.Ingesting)
            .ThenDo(_ => Status = ImportSessionStatus.Ingesting);

    public ErrorOr<Success> Complete(int rowCount, List<RowError> errors) =>
        Status
            .CanTransitionTo(ImportSessionStatus.Completed)
            .ThenDo(_ => Status = ImportSessionStatus.Completed)
            .ThenDo(_ => SuccessRowCount = rowCount)
            .ThenDo(_ => FailedRowCount = rowCount)
            .ThenDo(_ => RowErrors = errors)
            .ThenDo(_ => CompletedAt = DateTimeOffset.UtcNow);

    public ErrorOr<Success> Fail(string errorMessage) =>
        Status
            .CanTransitionTo(ImportSessionStatus.Failed)
            .ThenDo(_ => Status = ImportSessionStatus.Failed)
            .ThenDo(_ => ErrorMessage = errorMessage)
            .ThenDo(_ => CompletedAt = DateTimeOffset.UtcNow);
}
