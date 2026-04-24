using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Persistence;

public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookingEntity>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.CorrelationId).IsUnique();
            e.HasIndex(b => b.UserId);
            e.Property(b => b.Status).HasConversion<string>();
        });
    }
}