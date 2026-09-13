using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class HealthCheckTests
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
}
