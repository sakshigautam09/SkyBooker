using Microsoft.EntityFrameworkCore;
using SkyBooker.SeatService.Entities;

namespace SkyBooker.SeatService.Context;

public class SeatDbContext : DbContext
{
    public SeatDbContext(DbContextOptions<SeatDbContext> options) : base(options) { }

    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Seat>()
            .Property(s => s.SeatClass)
            .HasConversion<string>();

        modelBuilder.Entity<Seat>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Seat>()
            .Property(s => s.PriceMultiplier)
            .HasColumnType("decimal(5,2)");

        // Composite index for fast seat map lookup
        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.FlightId, s.SeatNumber })
            .IsUnique();

        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.FlightId, s.SeatClass });
    }
}
