using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification.Domain.Entities.Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification.Domain.Entities.Notification>().ToTable("Notifications");
        modelBuilder.Entity<Notification.Domain.Entities.Notification>().HasKey(x => x.Id);
    }
}
