using Microsoft.EntityFrameworkCore;
using ParkingLot.Domain.Entities;

namespace ParkingLot.Infrastructure.Persistence;

public class ParkingLotDbContext(DbContextOptions<ParkingLotDbContext> options) : DbContext(options)
{
    public DbSet<ParkingLotEntity> ParkingLots => Set<ParkingLotEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingLotEntity>(e =>
        {
            e.HasKey(p => p.LotId);
            e.Property(p => p.LotId).ValueGeneratedNever();
            e.Property(p => p.Name).IsRequired().HasMaxLength(150);
            e.Property(p => p.Address).IsRequired().HasMaxLength(300);
            e.Property(p => p.City).IsRequired().HasMaxLength(100);
            e.Property(p => p.State).IsRequired().HasMaxLength(100);
            e.Property(p => p.PinCode).IsRequired().HasMaxLength(10);
            e.Property(p => p.PricePerHour).HasPrecision(10, 2);
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(p => p.City);
            e.HasIndex(p => p.Status);
            e.HasIndex(p => p.ManagerId);
            e.HasIndex(p => new { p.Name, p.City }).IsUnique();  // prevents race-condition duplicates
        });
    }
}