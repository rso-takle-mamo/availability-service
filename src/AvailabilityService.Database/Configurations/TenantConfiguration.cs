using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AvailabilityService.Database.Entities;

namespace AvailabilityService.Database.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.BusinessName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Email)
            .HasMaxLength(255);

        builder.Property(t => t.Phone)
            .HasMaxLength(50);

        builder.Property(t => t.Address)
            .HasMaxLength(500);

        builder.Property(t => t.TimeZone)
            .HasMaxLength(50);

        builder.Property(t => t.BufferBeforeMinutes)
            .HasDefaultValue(0);

        builder.Property(t => t.BufferAfterMinutes)
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}