using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentEntity>       Payments       => Set<PaymentEntity>();
    public DbSet<RazorpayOrderEntity> RazorpayOrders => Set<RazorpayOrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentEntity>(e =>
        {
            e.HasKey(p => p.PaymentId);
            e.Property(p => p.PaymentId).ValueGeneratedOnAdd();
            e.Property(p => p.Amount).HasPrecision(10, 2);

            // Both nullable — only one will be set per payment
            e.Property(p => p.BookingId).IsRequired(false);
            e.Property(p => p.TicketId).IsRequired(false);

            e.Property(p => p.Mode)
             .HasConversion<string>()
             .HasMaxLength(10)
             .HasDefaultValue(Payment.Domain.Enums.PaymentMode.Cash);

            e.Property(p => p.Status)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.Property(p => p.TransactionId)
             .HasMaxLength(100)
             .IsRequired(false);

            e.Property(p => p.RefundedAt).IsRequired(false);

            // Indexes for both lookup paths
            e.HasIndex(p => p.BookingId);
            e.HasIndex(p => p.TicketId);
            e.HasIndex(p => p.Status);
        });

        modelBuilder.Entity<RazorpayOrderEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).ValueGeneratedOnAdd();
            e.Property(o => o.RazorpayOrderId).HasMaxLength(100).IsRequired();
            e.Property(o => o.Currency).HasMaxLength(10);
            e.Property(o => o.Status).HasMaxLength(20);
            e.HasIndex(o => o.RazorpayOrderId).IsUnique();
            e.HasIndex(o => o.CorrelationId).IsUnique();
        });
    }
}