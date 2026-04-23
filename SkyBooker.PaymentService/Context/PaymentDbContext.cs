using Microsoft.EntityFrameworkCore;
using SkyBooker.PaymentService.Entities;

namespace SkyBooker.PaymentService.Context;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentMode)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.RefundAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.BookingId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.UserId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.TransactionId)
            .IsUnique()
            .HasFilter("transaction_id IS NOT NULL");
    }
}
