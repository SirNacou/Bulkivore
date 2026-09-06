using System.Diagnostics;
using Bulkivore.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new(typeof(Worker).Namespace!);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client, null);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(() => db.Database.MigrateAsync(cancellationToken: stoppingToken));
        }
        catch (Exception e)
        {
            activity?.AddException(e);
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
