using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Sentri.Api.Domain;

namespace Sentri.Api.Infrastructure.Configurations;

public class ProviderMonthlySnapshotConfiguration : IEntityTypeConfiguration<ProviderMonthlySnapshot>
{
    public void Configure(EntityTypeBuilder<ProviderMonthlySnapshot> builder)
    {
        builder.ToTable("Snapshots");

        builder.HasKey(s => s.Id);
        builder.HasMany<Expense>()
            .WithOne()
            .HasForeignKey(e => e.SnapshotId)
            .IsRequired();

        builder.Property(s => s.TotalSpend)
            .HasPrecision(18, 2);

        builder.Property(s => s.Version).IsRowVersion();

        builder.HasIndex(s => new { s.ProviderId, s.Year, s.Month })
            .IsUnique();
    }
}
