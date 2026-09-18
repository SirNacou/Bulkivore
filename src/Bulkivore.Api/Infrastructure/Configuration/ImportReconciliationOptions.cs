namespace Bulkivore.Api.Infrastructure.Configuration;

public sealed class ImportReconciliationOptions
{
    public const string SectionName = "ImportReconciliation";

    /// <summary>
    /// How often the reconciliation job runs. Default: every 60 seconds.
    /// </summary>
    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A session is stale when IngestionStartedAt is older than this.
    /// Must comfortably exceed the longest legitimate import duration.
    /// </summary>
    public TimeSpan StaleThreshold { get; init; } = TimeSpan.FromHours(1);
}
