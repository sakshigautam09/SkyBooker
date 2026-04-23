using Microsoft.EntityFrameworkCore;
using SkyBooker.NotificationService.Entities;

namespace SkyBooker.NotificationService.Context;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Channel)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.RecipientId);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientId, n.IsRead });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.RelatedBookingId);
    }
}
