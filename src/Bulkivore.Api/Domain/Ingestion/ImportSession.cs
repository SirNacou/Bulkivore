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

    private Dictionary<string, string> _columnMappings;
    public IReadOnlyDictionary<string, string> ColumnMappings => _columnMappings;

    public ErrorOr<Success> ApplyMappings(
        IReadOnlyDictionary<string, string> mappings,
        IReadOnlyList<string> requiredTargetColumns)
    {
        if (Status is not (ImportSessionStatus.Uploaded or ImportSessionStatus.Mapped))
        {
            return Error.Validation(
                description: $"Cannot apply mappings when session is in '{Status}' status.",
                metadata: new() { [nameof(Status)] = Status.ToString() }
            );
        }

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
}
