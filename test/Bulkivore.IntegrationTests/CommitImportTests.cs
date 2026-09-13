using Bulkivore.Api.Endpoints.Imports.CommitImport;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class CommitImportTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task CommitImport_ProcessesAllRowsSuccessfully()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);
        await ImportTestHelper.MapDefaultColumnsAsync(client, sessionId);

        var (res, payload, err) =
            await client.PostAsync<CommitImportEndpoint, CommitImportRequest, CommitImportResponse>(
                new() { Id = sessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(x => x.SuccessCount, count => count.IsEqualTo(3))
            .And.Member(x => x.FailedCount, count => count.IsEqualTo(0));
    }
}
