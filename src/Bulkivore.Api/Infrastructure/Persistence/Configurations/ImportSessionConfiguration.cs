using System.Text.Json;
using System.Text.Json.Serialization;
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

        builder
            .Property(x => x.ColumnMappings)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v) ?? new Dictionary<string, string>()
            )
            .IsRequired();

        builder.Navigation(x => x.ColumnMappings).HasField("_columnMappings");

        // Indexes
        builder
            .HasIndex(x => new { x.TenantId, x.TargetTable })
            .HasDatabaseName("ix_import_sessions_tenant_target_table");
    }
}
