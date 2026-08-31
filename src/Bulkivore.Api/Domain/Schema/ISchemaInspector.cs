namespace Bulkivore.Api.Domain.Schema;

public interface ISchemaInspector
{
    Task<IReadOnlyDictionary<string, ColumnMetadata>> InspectTableAsync(
        string tableName,
        string schemaName = "public",
        CancellationToken ct = default);
}
