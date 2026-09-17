using Bulkivore.Api.Domain.Imports;
using Bulkivore.Api.Endpoints.Imports.SetImportMappings;
using Bulkivore.IntegrationTests.Common;
using Bulkivore.IntegrationTests.Fixtures;

namespace Bulkivore.IntegrationTests;

public class SetMappingsTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture AspireFixture { get; init; }

    [Test]
    public async Task SetMappings_AcceptsValidColumnMapping()
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);

        var (res, payload, err) =
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
                });

        await err.Should().BeNull();
        await res.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await payload.Should().NotBeNull();
    }
}
