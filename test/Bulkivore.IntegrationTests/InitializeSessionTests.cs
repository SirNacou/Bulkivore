using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.InitializeSession;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class InitializeSessionTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task InitializeSession_ReturnsSessionIdAndUploadUrl()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);

        var req = new InitializeSessionRequest
        {
            TargetTable = schema.TargetTable,
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

        using var csvContent = new StringContent(ImportTestHelper.DefaultCsvContent);
        var uploadResponse = await client.PutAsync(payload.UploadUrl, csvContent);
        await uploadResponse.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }
}
