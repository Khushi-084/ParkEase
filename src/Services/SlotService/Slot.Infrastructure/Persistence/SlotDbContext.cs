using Microsoft.EntityFrameworkCore;
using Slot.Domain.Entities;

namespace Slot.Infrastructure.Persistence;

public class SlotDbContext(DbContextOptions<SlotDbContext> options) : DbContext(options)
{
    public DbSet<SlotEntity> Slots => Set<SlotEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SlotEntity>(e =>
        {
            e.HasKey(s => s.SlotId);
            e.Property(s => s.SlotId).ValueGeneratedNever();
            e.Property(s => s.SlotNumber).IsRequired().HasMaxLength(20);
            e.Property(s => s.PricePerHour).HasPrecision(10, 2);
            e.Property(s => s.Type).HasConversion<string>().HasMaxLength(10);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            // Unique slot number per lot
            e.HasIndex(s => new { s.LotId, s.SlotNumber }).IsUnique();

            // Indexes for common queries
            e.HasIndex(s => s.LotId);
            e.HasIndex(s => s.Status);
            e.HasIndex(s => s.Type);
        });
    }
}