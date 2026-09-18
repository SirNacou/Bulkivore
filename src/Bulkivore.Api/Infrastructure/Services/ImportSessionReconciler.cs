using Bulkivore.Api.Domain.Imports;
using Bulkivore.Api.Infrastructure.Configuration;
using Bulkivore.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bulkivore.Api.Infrastructure.Services;

/// <summary>
/// Marks import sessions stuck in <see cref="ImportSessionStatus.Ingesting"/> as failed.
/// A crash between the COPY committing and the final SaveChangesAsync leaves a session in
/// Ingesting forever; rows may or may not exist, so the truthful terminal state is Failed.
/// A session whose ingestion started more than <see cref="ImportReconciliationOptions.StaleThreshold"/>
/// ago is considered dead — a live import cannot take that long to make its first bookkeeping write.
/// Stateless and short-lived: safe to invoke from the hosted job, tests, or an admin endpoint.
/// </summary>
public class ImportSessionReconciler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IOptions<ImportReconciliationOptions> options,
    ILogger<ImportSessionReconciler> logger
)
{
    public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
    {
        var staleBefore = DateTimeOffset.UtcNow - options.Value.StaleThreshold;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var staleSessions = await dbContext.ImportSessions
            .Where(s => s.Status == ImportSessionStatus.Ingesting
                        && s.IngestionStartedAt < staleBefore)
            .ToListAsync(cancellationToken);

        if (staleSessions.Count == 0)
            return 0;

        foreach (var session in staleSessions)
        {
            session.Fail("Import interrupted: ingestion stalled or the application restarted mid-import.");
            logger.LogWarning(
                "Reconciled stale import session {SessionId} for table {TargetTable} (ingestion started {StartedAt})",
                session.Id,
                session.TargetTable,
                session.IngestionStartedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return staleSessions.Count;
    }
}
