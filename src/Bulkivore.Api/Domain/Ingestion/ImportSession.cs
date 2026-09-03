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

    public ErrorOr<Success> MarkUploaded() =>
        Status
            .CanTransitionTo(ImportSessionStatus.Uploaded)
            .ThenDo(_ => Status = ImportSessionStatus.Uploaded);

    public ErrorOr<Success> TransitionTo(ImportSessionStatus newStatus) =>
        Status
            .CanTransitionTo(newStatus)
            .ThenDo(_ => Status = newStatus);
}
