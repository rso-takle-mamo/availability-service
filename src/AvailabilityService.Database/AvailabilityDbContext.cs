using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Configurations;

namespace AvailabilityService.Database;

public class AvailabilityDbContext : DbContext
{
    public DbSet<WorkingHours> WorkingHours { get; set; }
    public DbSet<TimeBlock> TimeBlocks { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Booking> Bookings { get; set; } // NOTE: Booking service not implemented yet - blueprint

    public AvailabilityDbContext() { }

    public AvailabilityDbContext(DbContextOptions<AvailabilityDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
    
        optionsBuilder.UseNpgsql(EnvironmentVariables.GetRequiredVariable("DATABASE_CONNECTION_STRING"));
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new WorkingHoursConfiguration());
        modelBuilder.ApplyConfiguration(new TimeBlockConfiguration());
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is WorkingHours || e.Entity is TimeBlock || e.Entity is Tenant || e.Entity is Booking 
                && (e.State is EntityState.Added or EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                    if (((dynamic)entityEntry.Entity).CreatedAt == default(DateTime))
                        ((dynamic)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    ((dynamic)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}