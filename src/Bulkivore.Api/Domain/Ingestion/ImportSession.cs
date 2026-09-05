using Bulkivore.Api.Domain.Common;
using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

[ValueObject<Guid>] public readonly partial record struct ImportSessionId;

public sealed class ImportSession : AggregateRoot<ImportSessionId>
{
    public string TenantId { get; init; }
    public string TargetTable { get; init; }
    public SourceFile File { get; init; }
    public string StorageKey { get; init; }
    public ImportSessionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    private Dictionary<string, string> _columnMappings;
    public IReadOnlyDictionary<string, string> ColumnMappings => _columnMappings;
    public int ProcessedRows => SuccessRowCount + FailedRowCount;
    public int SuccessRowCount { get; private set; }
    public int FailedRowCount { get; private set; }
    private List<RowError> _rowErrors;
    public IReadOnlyList<RowError> RowErrors => _rowErrors;
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ImportSession(string tenantId, string targetTable, SourceFile file, string storageKey)
        : base(ImportSessionId.FromNewVersion7Guid())
    {
        TenantId = tenantId;
        TargetTable = targetTable;
        File = file;
        StorageKey = storageKey;
        Status = ImportSessionStatus.Initialized;
        CreatedAt = DateTimeOffset.UtcNow;
        _columnMappings = [];
        _rowErrors = [];
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

        var errorOrFileName = SourceFile.TryFrom(fileName);
        if (!errorOrFileName.IsSuccess)
            errors.AddRange(Error.Validation(description: errorOrFileName.Error.ErrorMessage));

        if (errors.Count > 0) return errors;

        var importSession = new ImportSession(
            string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId,
            targetTable,
            errorOrFileName.ValueObject,
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

    public ErrorOr<Success> Complete(int rowCount, List<RowError> errors)
    {
        var errorOr = Status.CanTransitionTo(ImportSessionStatus.Completed);
        if (errorOr.IsError) return errorOr;

        Status = ImportSessionStatus.Completed;
        SuccessRowCount = rowCount;
        FailedRowCount = rowCount;
        _rowErrors = errors;
        CompletedAt = DateTimeOffset.UtcNow;
        return errorOr;
    }

    public ErrorOr<Success> Fail(string errorMessage)
    {
        var errorOr = Status.CanTransitionTo(ImportSessionStatus.Failed);
        if (errorOr.IsError) return errorOr;

        Status = ImportSessionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
        return errorOr;
    }
}
