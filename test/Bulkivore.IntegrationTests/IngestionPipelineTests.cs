using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.CommitImport;
using Bulkivore.Api.Endpoints.Imports.GetImportSession;
using Bulkivore.Api.Endpoints.Imports.InitializeSession;
using Bulkivore.Api.Endpoints.Imports.InspectSession;
using Bulkivore.Api.Endpoints.Imports.SetImportMappings;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;
using Npgsql;

namespace Bulkivore.IntegrationTests;

public class IngestionPipelineTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task HealthCheck_ReturnsOk()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var response = await client.GetAsync("/health");
        await response.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task InitializeImportSession_ReturnsPresignedUrlAndHeaders()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);

        var uniqueTenant = $"tenant_{Guid.NewGuid():N}";

        var req = new InitializeSessionRequest
        {
            TargetTable = schema.TargetTable,
            FileName = "test_products.csv",
            TenantId = uniqueTenant
        };

        var (initRes, initPayload, initErr) =
            await client.PostAsync<InitializeSessionEndpoint, InitializeSessionRequest, InitializeSessionResponse>(req);

        await initErr.Should().BeNull();
        await initRes.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await initPayload.Should().NotBeNull();
        var sessionId = initPayload.SessionId;

        using var csvContent = new StringContent(
            """
            sku,name,price,quantity
            SKU-001,Ergonomic Chair,249.99,15
            SKU-002,Mechanical Keyboard,129.50,42
            SKU-003,USB-C Dock,89.00,0
            """);
        var uploadResponse = await client.PutAsync(initPayload.UploadUrl, csvContent);
        await uploadResponse.StatusCode.Should().EqualTo(HttpStatusCode.OK);

        var (inspectRes, inspectPayload, inspectErr) =
            await client.GetAsync<InspectSessionEndpoint, InspectSessionRequest, InspectSessionResponse>(
                new()
                {
                    Id = sessionId
                });
        await inspectErr.Should().BeNull();
        await inspectRes.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(inspectPayload)
            .IsNotNull()
            .And.Member(
                x => x.Headers,
                headers => headers.IsEquivalentTo(["sku", "name", "price", "quantity"]));

        var mappingReq = new SetImportMappingsRequest
        {
            Id = sessionId,
            Mappings =
            [
                ColumnMapping.Create("sku", "sku").Value,
                ColumnMapping.Create("name", "name").Value,
                ColumnMapping.Create("price", "price").Value,
                ColumnMapping.Create("quantity", "quantity").Value
            ]
        };

        var (mapRes, mapPayload, mapErr) =
            await client.PostAsync<SetImportMappingsEndpoint, SetImportMappingsRequest, SetImportMappingsResponse>(
                mappingReq);

        await mapErr.Should().BeNull();
        await mapRes.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await mapPayload.Should().NotBeNull();

        var (commitRes, commitPayload, commitErr) =
            await client.PostAsync<CommitImportEndpoint, CommitImportRequest, CommitImportResponse>(
                new() { Id = sessionId });

        await commitErr.Should().BeNull();
        await commitRes.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(commitPayload)
            .IsNotNull()
            .And.Member(
                x => x.SuccessCount,
                successCount => successCount.IsEqualTo(3))
            .And.Member(
                x => x.FailedCount,
                failedCount => failedCount.IsEqualTo(0));

        var (getRes, getPayload, getErr) =
            await client.GetAsync<GetImportSessionEndpoint, GetImportSessionRequest, GetImportSessionResponse>(
                new() { Id = sessionId });

        await getErr.Should().BeNull();
        await getRes.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(getPayload)
            .IsNotNull()
            .And.Member(
                x => x.SuccessRowCount,
                rowCount => rowCount.IsEqualTo(3));
    }
}
