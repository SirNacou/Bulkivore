using Bulkivore.Api.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bulkivore.Api.Infrastructure.Persistence.Configurations;

public class ImportSessionConfiguration : IEntityTypeConfiguration<ImportSession>
{
    public void Configure(EntityTypeBuilder<ImportSession> builder)
    {
        // Table & Primary Key
        builder.HasKey(x => x.Id);

        // Properties & Constraints
        builder
            .Property(x => x.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(x => x.TargetTable)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(x => x.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder
            .Property(x => x.CreatedAt)
            .IsRequired();

        // Indexes
        builder
            .HasIndex(x => new { x.TenantId, x.TargetTable })
            .HasDatabaseName("ix_import_sessions_tenant_target_table");
    }
}
