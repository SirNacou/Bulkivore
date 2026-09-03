using System.Reflection;
using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Infrastructure.Persistence;

public sealed class BulkivoreDbContext(DbContextOptions<BulkivoreDbContext> options) : DbContext(options)
{
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.RegisterAllInVogenEfCoreConverters();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
