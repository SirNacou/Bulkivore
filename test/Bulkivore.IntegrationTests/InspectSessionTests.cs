using Bulkivore.Api.Endpoints.Imports.InspectSession;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class InspectSessionTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task InspectSession_ReturnsExpectedHeaders()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);

        var (res, payload, err) =
            await client.GetAsync<InspectSessionEndpoint, InspectSessionRequest, InspectSessionResponse>(
                new() { Id = sessionId });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await Assert.That(payload)
            .IsNotNull()
            .And.Member(
                x => x.Headers,
                headers => headers.IsEquivalentTo(["sku", "name", "price", "quantity"]));
    }
}
