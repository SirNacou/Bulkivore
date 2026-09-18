using Bulkivore.Api.Infrastructure.Configuration;
using Bulkivore.Api.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace Bulkivore.Api.Infrastructure.Services;

/// <summary>
/// Thin hosted shell around <see cref="ImportSessionReconciler"/>: runs the reconciliation
/// sweep once at startup (catching sessions orphaned by a restart) and then on a fixed
/// interval (catching sessions orphaned by a worker-level crash while the process lives on).
/// All real logic lives in the injectable singleton service; this class only schedules.
/// </summary>
public class ImportSessionReconcilerJob(
    ImportSessionReconciler reconciler,
    IOptions<ImportReconciliationOptions> options,
    ILogger<ImportSessionReconcilerJob> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield so startup (migrations, health checks) isn't blocked behind DB access.
        await Task.Yield();

        var interval = options.Value.SweepInterval;

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                var count = await reconciler.ReconcileAsync(stoppingToken);
                if (count > 0)
                    logger.LogInformation("Reconciled {Count} stale import session(s).", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never take the app down over bookkeeping reconciliation.
                logger.LogError(ex, "Import session reconciliation sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
