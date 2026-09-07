using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Endpoints.Imports.InitializeSession;
using Bulkivore.Api.Endpoints.Imports.InspectSession;
using Bulkivore.IntegrationTests.Common;

namespace Bulkivore.IntegrationTests;

public class IngestionPipelineTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public required AppFixture App { get; init; }


    [Test]
    public async Task HealthCheck_ReturnsOk()
    {
        var client = App.CreateHttpClient("api");

        var response = await client.GetAsync("/health");
        await response.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task InitializeImportSession_ReturnsPresignedUrlAndHeaders()
    {
        var client = App.CreateHttpClient("api");

        var req = new InitializeSessionRequest
        {
            TargetTable = "products",
            FileName = "test_products.csv",
            TenantId = "tenant_test"
        };

        var (response, payload, errorMessage) =
            await client.PostAsync<InitializeSessionEndpoint, InitializeSessionRequest, InitializeSessionResponse>(req);

        await response.StatusCode.Should().EqualTo(HttpStatusCode.OK);
        await payload.Should().NotBeNull();
        await payload.SessionId.Should().NotBeEqualTo(ImportSessionId.Empty);
        await payload.UploadUrl.Should().NotBeEmpty();

        using var csvContent = new StringContent(
            """
            sku,name,price,quantity
            SKU-1,Widget,10.50,100
            """
        );
        var uploadResponse = await client.PutAsync(payload.UploadUrl, csvContent);

        await uploadResponse.StatusCode.Should().EqualTo(HttpStatusCode.OK);

        var (inspectResponse, inspectPayload, inspectErrorMessage) =
            await client.GetAsync<InspectSessionEndpoint, InspectSessionRequest, InspectSessionResponse>(
                new()
                {
                    SessionId = payload.SessionId
                }
            );
        await inspectResponse.StatusCode.Should().EqualTo(HttpStatusCode.OK);
    }
}
