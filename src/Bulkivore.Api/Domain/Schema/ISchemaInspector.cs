namespace Bulkivore.Api.Domain.Schema;

public interface ISchemaInspector
{
    Task<ErrorOr<TableSchema>> InspectTableAsync(
        string tableName,
        string schemaName = "public",
        CancellationToken ct = default);
}
