using Microsoft.EntityFrameworkCore;
using SkyBooker.BookingService.Entities;

namespace SkyBooker.BookingService.Context;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Booking>()
            .Property(b => b.TripType)
            .HasConversion<string>();

        modelBuilder.Entity<Booking>()
            .Property(b => b.BaseFare)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.Taxes)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.AncillaryCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalFare)
            .HasColumnType("decimal(18,2)");

        // PNR must be unique
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.PnrCode)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.UserId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.FlightId);
    }
}
