using Aspire.Hosting;
using Npgsql;

namespace Bulkivore.IntegrationTests.Fixtures;

public sealed class TestSchemaScope : IAsyncDisposable
{
    public string SchemaName { get; }
    public string TargetTable => $"{SchemaName}.products";

    private readonly NpgsqlDataSource _dataSource;

    private TestSchemaScope(string schemaName, NpgsqlDataSource dataSource)
    {
        SchemaName = schemaName;
        _dataSource = dataSource;
    }

    public static async Task<TestSchemaScope> CreateAsync(DistributedApplication app)
    {
        // 1. Resolve dynamic port/connection string exposed by Aspire
        var connectionString = await app.GetConnectionStringAsync("bulkivore-test-db")
                               ?? throw new InvalidOperationException(
                                   "Connection string for 'bulkivore-test-db' was not found in Aspire AppHost.");

        var dataSource = NpgsqlDataSource.Create(connectionString);
        var schemaName = $"test_{Guid.NewGuid():N}";

        // 2. Create isolated PostgreSQL schema and table
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
                           CREATE SCHEMA "{schemaName}";
                           CREATE TABLE "{schemaName}".products (
                               id SERIAL PRIMARY KEY,
                               sku VARCHAR(100) NOT NULL UNIQUE,
                               name VARCHAR(255) NOT NULL,
                               price NUMERIC(10, 2) NOT NULL,
                               quantity INT NOT NULL,
                               imported_at TIMESTAMPTZ DEFAULT NOW()
                           );
                           """;
        await cmd.ExecuteNonQueryAsync();

        return new TestSchemaScope(schemaName, dataSource);
    }

    public async ValueTask DisposeAsync()
    {
        // 3. Drop isolated schema and dispose data source pool
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""DROP SCHEMA IF EXISTS "{SchemaName}" CASCADE;""";
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await _dataSource.DisposeAsync();
        }
    }
}
