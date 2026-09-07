using Bulkivore.Api.Domain.Schema;
using Npgsql;

namespace Bulkivore.Api.Infrastructure.Services;

public class PostgresSchemaInspector(IConfiguration configuration) : ISchemaInspector
{
    private readonly string _connectionString = configuration.GetConnectionString("bulkivore-test-db")
                                                ?? throw new InvalidOperationException(
                                                    "Missing connection string for test database"
                                                );

    public async Task<ErrorOr<TableSchema>> InspectTableAsync(
        string tableName,
        string schemaName = "public",
        CancellationToken ct = default)
    {
        var columnList = new List<ColumnMetadata>();

        const string sql =
            """
            SELECT
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.character_maximum_length,
                COALESCE(c.is_identity, 'NO') AS is_identity,
                c.column_default
            FROM information_schema.columns c
            WHERE c.table_schema = @schema
              AND c.table_name = @table
            ORDER BY c.ordinal_position;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("schema", schemaName);
        cmd.Parameters.AddWithValue("table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0);
            var rawDataType = reader.GetString(1);
            var isNullable = reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase);
            var maxLength = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            var isIdentity = reader.GetString(4).Equals("YES", StringComparison.OrdinalIgnoreCase);
            var defaultValue = reader.IsDBNull(5) ? null : reader.GetString(5);

            var hasDefault = !string.IsNullOrWhiteSpace(defaultValue);
            var isGenerated = isIdentity
                              || (defaultValue?.StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ?? false);

            var domainType = MapToDomainType(rawDataType);

            var metadataResult = ColumnMetadata.Create(
                name: name,
                dataType: domainType,
                isNullable: isNullable,
                maxLength: maxLength,
                isIdentity: isIdentity,
                hasDefault: hasDefault,
                isGenerated: isGenerated
            );

            if (metadataResult.IsError)
            {
                throw new InvalidOperationException(metadataResult.FirstError.Description);
            }

            columnList.Add(metadataResult.Value);
        }

        return columnList.Count == 0
            ? Error.NotFound(description: $"Table '{schemaName}.{tableName}' does not exist or has no columns.")
            : TableSchema.Create(tableName, schemaName, columnList);
    }

    private static ColumnDataType MapToDomainType(string sqlDataType) =>
        sqlDataType.ToLowerInvariant() switch
        {
            "integer" or "int" or "int4" or "smallint" or "int2" => ColumnDataType.Integer,
            "bigint" or "int8" => ColumnDataType.BigInt,
            "numeric" or "decimal" or "money" or "real" or "double precision" => ColumnDataType.Decimal,
            "boolean" or "bool" => ColumnDataType.Boolean,
            "timestamp with time zone" or "timestamptz" or "timestamp without time zone" => ColumnDataType.DateTime,
            "date" => ColumnDataType.Date,
            "uuid" => ColumnDataType.Uuid,
            "json" or "jsonb" => ColumnDataType.Json,
            "bytea" => ColumnDataType.Binary,
            _ => ColumnDataType.Text
        };
}
