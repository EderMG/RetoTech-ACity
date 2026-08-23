using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Venue).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Date);

        builder.HasMany(e => e.Zones)
            .WithOne()
            .HasForeignKey(z => z.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Zones).UsePropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
    }
}
