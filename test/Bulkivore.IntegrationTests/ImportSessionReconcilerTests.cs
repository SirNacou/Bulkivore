using Bulkivore.Api.Domain.Imports;
using Bulkivore.Api.Infrastructure.Persistence;
using Bulkivore.Api.Infrastructure.Services;
using Bulkivore.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.IntegrationTests;

public class ImportSessionReconcilerTests
{
    [ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
    public static AppFixture AspireFixture { get; set; } = null!;

    private static InfrastructureFixture? _infrastructure;

    private static InfrastructureFixture Infrastructure =>
        _infrastructure ?? throw new InvalidOperationException("Fixture not initialized.");

    [Before(Class)]
    public static async Task SetupInfrastructureAsync()
    {
        var connectionString = await AspireFixture.App.GetConnectionStringAsync("bulkivore-db")
                               ?? throw new InvalidOperationException(
                                   "Connection string for 'bulkivore-db' was not found in Aspire AppHost.");
        _infrastructure = new InfrastructureFixture(connectionString);
    }

    [After(Class)]
    public static async Task TeardownInfrastructureAsync()
    {
        if (_infrastructure is not null)
            await _infrastructure.DisposeAsync();
        _infrastructure = null;
    }

    private async Task<ImportSessionId> CreateIngestingSessionAsync(TimeSpan ingestionAge)
    {
        var client = AspireFixture.CreateHttpClient("api");
        await using var schema = await TestSchemaScope.CreateAsync(AspireFixture.App);
        var sessionId = await ImportTestHelper.InitializeSessionAsync(client, schema.TargetTable);
        await ImportTestHelper.MapDefaultColumnsAsync(client, sessionId);

        await using var dbContext = await Infrastructure
            .GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync();

        var session = await dbContext.ImportSessions.FirstAsync(x => x.Id == sessionId);
        session.Status = ImportSessionStatus.Ingesting;
        dbContext.Entry(session).Property(x => x.IngestionStartedAt).CurrentValue =
            DateTimeOffset.UtcNow - ingestionAge;
        await dbContext.SaveChangesAsync();

        return sessionId;
    }

    private async Task<ImportSession> LoadSessionAsync(ImportSessionId sessionId)
    {
        await using var dbContext = await Infrastructure
            .GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync();
        return await dbContext.ImportSessions.AsNoTracking().FirstAsync(x => x.Id == sessionId);
    }

    [Test]
    public async Task Reconciler_MarksStaleIngestingSessionAsFailed()
    {
        var sessionId = await CreateIngestingSessionAsync(TimeSpan.FromHours(2));

        // Resolve straight from the shared provider — singleton, no scope needed.
        var reconciler = Infrastructure.GetRequiredService<ImportSessionReconciler>();
        await reconciler.ReconcileAsync(CancellationToken.None);

        var reconciled = await LoadSessionAsync(sessionId);

        await Assert.That(reconciled.Status).IsEqualTo(ImportSessionStatus.Failed);
        await Assert.That(reconciled.ErrorMessage).IsNotNull().And.Contains("interrupted");
    }

    [Test]
    public async Task Reconciler_LeavesLiveIngestingSessionAlone()
    {
        var sessionId = await CreateIngestingSessionAsync(TimeSpan.FromSeconds(30));

        var reconciler = Infrastructure.GetRequiredService<ImportSessionReconciler>();
        await reconciler.ReconcileAsync(CancellationToken.None);

        var untouched = await LoadSessionAsync(sessionId);

        await Assert.That(untouched.Status).IsEqualTo(ImportSessionStatus.Ingesting);
    }
}
