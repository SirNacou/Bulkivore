using Bulkivore.Api.Domain.Common;
using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

[ValueObject<Guid>] public readonly partial struct ImportSessionId;

public sealed class ImportSession : AggregateRoot<ImportSessionId>
{
    public string TenantId { get; set; }
    public string TargetTable { get; set; }
    public string FileName { get; set; }
    public string StorageKey { get; set; }
    public ImportSessionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    private ImportSession(string tenantId, string targetTable, string fileName, string storageKey)
        : base(ImportSessionId.FromNewVersion7Guid())
    {
        TenantId = tenantId;
        TargetTable = targetTable;
        FileName = fileName;
        StorageKey = storageKey;
        Status = ImportSessionStatus.Initialized;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static ErrorOr<ImportSession> Initialize(string? tenantId, string targetTable, string fileName,
        string storageKey)
    {
        List<Error> errors = [];
        if (string.IsNullOrWhiteSpace(targetTable))
        {
            errors.Add(Error.Validation(description: "Target table name cannot be empty.", metadata:
                new() { [nameof(targetTable)] = targetTable }));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add(Error.Validation(description: "File name cannot be empty.", metadata:
                new() { [nameof(fileName)] = fileName }));
        }

        if (errors.Count > 0)
            return errors;

        var importSession = new ImportSession(
            string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId,
            targetTable,
            fileName,
            storageKey);

        return importSession;
    }

    public ErrorOr<Success> MarkUploaded() =>
        Status.CanTransitionTo(ImportSessionStatus.Uploaded)
            .ThenDo(_ => Status = ImportSessionStatus.Uploaded);

    public ErrorOr<Success> TransitionTo(ImportSessionStatus newStatus) =>
        Status.CanTransitionTo(newStatus)
            .ThenDo(_ => Status = newStatus);
}
