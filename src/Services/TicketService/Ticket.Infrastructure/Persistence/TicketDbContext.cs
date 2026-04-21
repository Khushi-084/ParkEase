using Microsoft.EntityFrameworkCore;
using Ticket.Domain.Entities;

namespace Ticket.Infrastructure.Persistence;

public class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketEntity>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.VehicleNumber)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(t => t.Amount)
                  .HasColumnType("decimal(10,2)");

            entity.Property(t => t.Status)
                  .HasConversion<string>();

            entity.HasIndex(t => t.VehicleNumber);
            entity.HasIndex(t => t.SlotId);
        });
    }
}