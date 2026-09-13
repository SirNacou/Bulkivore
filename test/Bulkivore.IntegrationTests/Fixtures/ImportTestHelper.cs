using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.CommitImport;
using Bulkivore.Api.Endpoints.Imports.InitializeSession;
using Bulkivore.Api.Endpoints.Imports.SetImportMappings;
using Bulkivore.IntegrationTests.Common;

namespace Bulkivore.IntegrationTests.Fixtures;

public static class ImportTestHelper
{
    public const string DefaultCsvContent = """
                                            sku,name,price,quantity
                                            SKU-001,Ergonomic Chair,249.99,15
                                            SKU-002,Mechanical Keyboard,129.50,42
                                            SKU-003,USB-C Dock,89.00,0
                                            """;

    public static Task<TestSchemaScope> CreateSchemaAsync(AppFixture fixture) =>
        TestSchemaScope.CreateAsync(fixture.App);

    public static async Task<ImportSessionId> InitializeSessionAsync(
        HttpClient client,
        string targetTable,
        string csvContent = DefaultCsvContent,
        CancellationToken ct = default)
    {
        var req = new InitializeSessionRequest
        {
            TargetTable = targetTable,
            FileName = "test_products.csv",
            TenantId = $"tenant_{Guid.NewGuid():N}"
        };

        var (res, payload, err) =
            await client.PostAsync<InitializeSessionEndpoint, InitializeSessionRequest, InitializeSessionResponse>(req, ct);

        if (!res.IsSuccessStatusCode || payload is null)
            throw new InvalidOperationException($"InitializeSession failed: {res.StatusCode} {err}");

        using var content = new StringContent(csvContent);
        var uploadResponse = await client.PutAsync(payload.UploadUrl, content, ct);
        uploadResponse.EnsureSuccessStatusCode();

        return payload.SessionId;
    }

    public static async Task MapDefaultColumnsAsync(
        HttpClient client,
        ImportSessionId sessionId,
        CancellationToken ct = default)
    {
        var (res, _, err) =
            await client.PostAsync<SetImportMappingsEndpoint, SetImportMappingsRequest, SetImportMappingsResponse>(
                new()
                {
                    Id = sessionId,
                    Mappings =
                    [
                        ColumnMapping.Create("sku", "sku").Value,
                        ColumnMapping.Create("name", "name").Value,
                        ColumnMapping.Create("price", "price").Value,
                        ColumnMapping.Create("quantity", "quantity").Value
                    ]
                },
                ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"SetImportMappings failed: {res.StatusCode} {err}");
    }

    public static async Task CommitAsync(
        HttpClient client,
        ImportSessionId sessionId,
        CancellationToken ct = default)
    {
        var (res, _, err) =
            await client.PostAsync<CommitImportEndpoint, CommitImportRequest, CommitImportResponse>(
                new() { Id = sessionId },
                ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"CommitImport failed: {res.StatusCode} {err}");
    }
}
