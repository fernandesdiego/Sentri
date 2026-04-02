using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentri.Api.Domain;

namespace Sentri.Api.Infrastructure.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.MonthlyBudget)
            .HasPrecision(18, 2);

        builder.Property(p => p.WarningThreshold)
            .HasDefaultValue(0.8m)
            .HasPrecision(4, 2);

        builder.Property(p => p.CurrentSpend)
            .HasPrecision(18, 2);
    }
}