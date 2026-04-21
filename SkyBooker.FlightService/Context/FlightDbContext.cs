using Microsoft.EntityFrameworkCore;
using SkyBooker.FlightService.Entities;

namespace SkyBooker.FlightService.Context;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options) { }

    public DbSet<Flight> Flights => Set<Flight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flight>()
            .Property(f => f.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Flight>()
            .Property(f => f.BasePrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Flight>()
            .Property(f => f.RowVersion)
            .IsRowVersion();
    }
}
