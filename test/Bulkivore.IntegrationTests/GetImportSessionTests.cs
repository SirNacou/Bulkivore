using Bulkivore.Api.Endpoints.Imports.GetImportSession;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class GetImportSessionTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task GetImportSession_ReturnsCorrectRowCount()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);
        await ImportTestHelper.MapDefaultColumnsAsync(client, sessionId);
        await ImportTestHelper.CommitAsync(client, sessionId);

        var (res, payload, err) =
            await client.GetAsync<GetImportSessionEndpoint, GetImportSessionRequest, GetImportSessionResponse>(
                new() { Id = sessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(x => x.SuccessRowCount, count => count.IsEqualTo(3));
    }
}
