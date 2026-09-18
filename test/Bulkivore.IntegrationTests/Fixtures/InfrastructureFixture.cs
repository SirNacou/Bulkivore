using Bulkivore.Api.Infrastructure;
using Bulkivore.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bulkivore.IntegrationTests.Fixtures;

/// <summary>
/// Builds a DI provider from the same composable registration modules the API uses
/// (AddPersistence + AddStorage + AddReconciliation), pointed at the test database.
/// Deliberately does NOT call AddInfrastructure(), so no hosted services are registered
/// and tests can invoke services like ImportSessionReconciler directly and deterministically.
/// Register once per test class via TUnit ClassDataSource; tests pull services with
/// GetRequiredService — no manual ServiceCollection, no scopes.
/// </summary>
public class InfrastructureFixture : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public InfrastructureFixture(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bulkivore-db"] = connectionString,
                ["Storage:BucketName"] = "bulkivore-imports",
                ["Storage:AccessKey"] = "test",
                ["Storage:SecretKey"] = "test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddPersistence();
        services.AddStorage();
        services.AddReconciliation();

        _provider = services.BuildServiceProvider();
    }

    public T GetRequiredService<T>() where T : notnull =>
        _provider.GetRequiredService<T>();

    public async ValueTask DisposeAsync() =>
        await _provider.DisposeAsync();
}
