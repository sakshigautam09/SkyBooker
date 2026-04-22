using Microsoft.EntityFrameworkCore;
using SkyBooker.PassengerService.Entities;

namespace SkyBooker.PassengerService.Context;

public class PassengerDbContext : DbContext
{
    public PassengerDbContext(DbContextOptions<PassengerDbContext> options) : base(options) { }

    public DbSet<PassengerInfo> Passengers => Set<PassengerInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PassengerInfo>()
            .Property(p => p.PassengerType)
            .HasConversion<string>();

        // Unique ticket number
        modelBuilder.Entity<PassengerInfo>()
            .HasIndex(p => p.TicketNumber)
            .IsUnique()
            .HasFilter("ticket_number IS NOT NULL");

        // Unique passport per booking
        modelBuilder.Entity<PassengerInfo>()
            .HasIndex(p => new { p.BookingId, p.PassportNumber })
            .IsUnique();

        modelBuilder.Entity<PassengerInfo>()
            .HasIndex(p => p.BookingId);

        modelBuilder.Entity<PassengerInfo>()
            .HasIndex(p => p.PassportNumber);
    }
}
