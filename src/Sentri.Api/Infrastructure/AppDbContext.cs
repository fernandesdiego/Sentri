using MediatR;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Domain;

namespace Sentri.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ProviderMonthlySnapshot> Snapshots => Set<ProviderMonthlySnapshot>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Provider>()
            .Metadata.FindNavigation(nameof(Provider.Snapshots))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}