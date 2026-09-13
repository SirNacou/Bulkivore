using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.ListImports;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class ListImportsTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task ListImports_ReturnsCreatedSession()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);

        var (res, payload, err) =
            await client.GetAsync<ListImportsEndpoint, ListImportsRequest, ListImportsResponse>(
                new() { Page = 1, PageSize = 100 });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.Page).IsEqualTo(1);
        await Assert.That(payload.PageSize).IsEqualTo(100);
        await Assert.That(payload.TotalCount >= 1).IsTrue();
        await Assert.That(payload.Items.Any(x => x.Id == sessionId)).IsTrue();
    }

    [Test]
    public async Task ListImports_SupportsPagination()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);
        await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);

        var (res1, page1, err1) =
            await client.GetAsync<ListImportsEndpoint, ListImportsRequest, ListImportsResponse>(
                new() { Page = 1, PageSize = 1 });

        await err1.Should().BeNull();
        await res1.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(page1).IsNotNull();
        await Assert.That(page1.Items.Count).IsEqualTo(1);
        await Assert.That(page1.TotalCount >= 2).IsTrue();

        var (res2, page2, err2) =
            await client.GetAsync<ListImportsEndpoint, ListImportsRequest, ListImportsResponse>(
                new() { Page = 2, PageSize = 1 });

        await err2.Should().BeNull();
        await res2.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(page2).IsNotNull();
        await Assert.That(page2.Items.Count).IsEqualTo(1);
        await Assert.That(page2.Items[0].Id).IsNotEqualTo(page1.Items[0].Id);
    }

    [Test]
    public async Task ListImports_FiltersByStatus()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);
        await ImportTestHelper.MapDefaultColumnsAsync(client, sessionId);
        await ImportTestHelper.CommitAsync(client, sessionId);

        var (res, payload, err) =
            await client.GetAsync<ListImportsEndpoint, ListImportsRequest, ListImportsResponse>(
                new() { Page = 1, PageSize = 100, Status = ImportSessionStatus.Completed });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.Items.Any(x => x.Id == sessionId)).IsTrue();
        await Assert.That(payload.Items.All(x => x.Status == ImportSessionStatus.Completed)).IsTrue();
    }
}
