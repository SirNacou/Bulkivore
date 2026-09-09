using Bulkivore.Api.Domain.Schema;
using Npgsql;

namespace Bulkivore.Api.Infrastructure.Services;

public class PostgresSchemaInspector([FromKeyedServices("bulkivore-test-db")] NpgsqlDataSource dataSource)
    : ISchemaInspector
{
    public async Task<ErrorOr<TableSchema>> InspectTableAsync(
        string tableName,
        string schemaName = "public",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return Error.Validation(description: "Table name cannot be empty.");
        }

        // Auto-detect schema-qualified table names (e.g., "test_a1b2.products")
        if (tableName.Contains('.'))
        {
            var parts = tableName.Split('.', 2);
            schemaName = parts[0];
            tableName = parts[1];
        }

        var columnList = new List<ColumnMetadata>();

        const string sql =
            """
            SELECT
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.character_maximum_length,
                COALESCE(c.is_identity, 'NO') AS is_identity,
                c.column_default,
                c.is_generated
            FROM information_schema.columns c
            WHERE LOWER(c.table_schema) = LOWER(@schema)
              AND LOWER(c.table_name) = LOWER(@table)
            ORDER BY c.ordinal_position;
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("schema", schemaName.Trim());
        cmd.Parameters.AddWithValue("table", tableName.Trim());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(reader.GetOrdinal("column_name"));
            var dataType = MapToDomainType(reader.GetString(reader.GetOrdinal("data_type")));
            var isNullable = reader.GetString(reader.GetOrdinal("is_nullable")) == "YES";
            var maxLengthOrdinal = reader.GetOrdinal("character_maximum_length");
            int? maxLength = reader.IsDBNull(maxLengthOrdinal)
                ? null
                : reader.GetInt32(maxLengthOrdinal);
            var isIdentity = reader.GetString(reader.GetOrdinal("is_identity")) == "YES";
            var hasDefault = !reader.IsDBNull(reader.GetOrdinal("column_default"));
            var isGenerated = reader.GetString(reader.GetOrdinal("is_generated")) == "ALWAYS";

            columnList.Add(
                new ColumnMetadata(name, dataType, isNullable, maxLength, isIdentity, hasDefault, isGenerated));
        }

        return columnList.Count == 0
            ? Error.NotFound(description: $"Table '{schemaName}.{tableName}' does not exist or has no columns.")
            : TableSchema.Create(tableName.Trim(), schemaName.Trim(), columnList);
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
