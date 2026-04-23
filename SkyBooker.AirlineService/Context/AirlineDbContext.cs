using Microsoft.EntityFrameworkCore;
using SkyBooker.AirlineService.Entities;

namespace SkyBooker.AirlineService.Context;

public class AirlineDbContext : DbContext
{
    public AirlineDbContext(DbContextOptions<AirlineDbContext> options)
        : base(options) { }

    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<AirlineAirport> AirlineAirports => Set<AirlineAirport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK for the join table
        modelBuilder.Entity<AirlineAirport>()
            .HasKey(aa => new { aa.AirlineId, aa.AirportId });

        modelBuilder.Entity<AirlineAirport>()
            .HasOne(aa => aa.Airline)
            .WithMany(a => a.AirlineAirports)
            .HasForeignKey(aa => aa.AirlineId);

        modelBuilder.Entity<AirlineAirport>()
            .HasOne(aa => aa.Airport)
            .WithMany(ap => ap.AirlineAirports)
            .HasForeignKey(aa => aa.AirportId);
    }
}
