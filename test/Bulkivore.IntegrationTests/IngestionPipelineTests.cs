using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.CommitImport;
using Bulkivore.Api.Endpoints.Imports.GetImportSession;
using Bulkivore.Api.Endpoints.Imports.InitializeSession;
using Bulkivore.Api.Endpoints.Imports.InspectSession;
using Bulkivore.Api.Endpoints.Imports.SetImportMappings;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class PipelineState : IAsyncDisposable
{
    public TestSchemaScope Schema { get; set; } = null!;
    public ImportSessionId SessionId { get; set; }

    public async ValueTask DisposeAsync()
    {
        await Schema.DisposeAsync();
    }
}

public class IngestionPipelineTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [ClassDataSource<PipelineState>(Shared = SharedType.PerTestSession)]
    public required PipelineState State { get; init; }

    private const string CsvContent = """
                                      sku,name,price,quantity
                                      SKU-001,Ergonomic Chair,249.99,15
                                      SKU-002,Mechanical Keyboard,129.50,42
                                      SKU-003,USB-C Dock,89.00,0
                                      """;

    [Test]
    public async Task HealthCheck_ReturnsOk()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var response = await client.GetAsync("/health");
        await response.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task InitializeSession_ReturnsSessionIdAndUploadUrl()
    {
        var client = AspireFixture.CreateHttpClient("api");
        State.Schema = await TestSchemaScope.CreateAsync(AspireFixture.App);

        var req = new InitializeSessionRequest
        {
            TargetTable = State.Schema.TargetTable,
            FileName = "test_products.csv",
            TenantId = $"tenant_{Guid.NewGuid():N}"
        };

        var (res, payload, err) =
            await client.PostAsync<InitializeSessionEndpoint, InitializeSessionRequest, InitializeSessionResponse>(req);

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.SessionId).IsNotEqualTo(ImportSessionId.Empty);
        await Assert.That(payload.UploadUrl).IsNotEmpty();

        State.SessionId = payload.SessionId;

        using var csvContent = new StringContent(CsvContent);
        var uploadResponse = await client.PutAsync(payload.UploadUrl, csvContent);
        await uploadResponse.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }

    [Test, DependsOn(nameof(InitializeSession_ReturnsSessionIdAndUploadUrl))]
    public async Task InspectSession_ReturnsExpectedHeaders()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var (res, payload, err) =
            await client.GetAsync<InspectSessionEndpoint, InspectSessionRequest, InspectSessionResponse>(
                new() { Id = State.SessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(
                x => x.Headers,
                headers => headers.IsEquivalentTo(["sku", "name", "price", "quantity"]));
    }

    [Test, DependsOn(nameof(InspectSession_ReturnsExpectedHeaders))]
    public async Task SetMappings_AcceptsValidColumnMapping()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var (res, payload, err) =
            await client.PostAsync<SetImportMappingsEndpoint, SetImportMappingsRequest, SetImportMappingsResponse>(
                new()
                {
                    Id = State.SessionId,
                    Mappings =
                    [
                        ColumnMapping.Create("sku", "sku").Value,
                        ColumnMapping.Create("name", "name").Value,
                        ColumnMapping.Create("price", "price").Value,
                        ColumnMapping.Create("quantity", "quantity").Value
                    ]
                });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await payload.Should().NotBeNull();
    }

    [Test, DependsOn(nameof(SetMappings_AcceptsValidColumnMapping))]
    public async Task CommitImport_ProcessesAllRowsSuccessfully()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var (res, payload, err) =
            await client.PostAsync<CommitImportEndpoint, CommitImportRequest, CommitImportResponse>(
                new() { Id = State.SessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(x => x.SuccessCount, count => count.IsEqualTo(3))
            .And.Member(x => x.FailedCount, count => count.IsEqualTo(0));
    }

    [Test, DependsOn(nameof(CommitImport_ProcessesAllRowsSuccessfully))]
    public async Task GetImportSession_ReturnsCorrectRowCount()
    {
        var client = AspireFixture.CreateHttpClient("api");

        var (res, payload, err) =
            await client.GetAsync<GetImportSessionEndpoint, GetImportSessionRequest, GetImportSessionResponse>(
                new() { Id = State.SessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(x => x.SuccessRowCount, count => count.IsEqualTo(3));
    }
}
