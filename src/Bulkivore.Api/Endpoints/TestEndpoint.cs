using System.Text.Json;
using FastEndpoints;
using Npgsql;

namespace Bulkivore.Api.Endpoints;

public class TestEndpoint(NpgsqlDataSource dataSource) : Ep.NoReq.NoRes
{
	public override void Configure()
	{
		Get("/test");
		AllowAnonymous();
	}

	public override Task HandleAsync(CancellationToken ct)
	{
		return Send.OkAsync(JsonSerializer.Serialize(dataSource), ct);
	}
}