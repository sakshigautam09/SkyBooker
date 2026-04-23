using Microsoft.EntityFrameworkCore;
using SkyBooker.PaymentService.Context;
using SkyBooker.PaymentService.Entities;
using SkyBooker.PaymentService.Enums;

namespace SkyBooker.PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> FindByBookingIdAsync(string bookingId)
        => await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);

    public async Task<IList<Payment>> FindByUserIdAsync(int userId)
        => await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<IList<Payment>> FindByStatusAsync(string status)
    {
        if (!Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            return new List<Payment>();

        return await _context.Payments
            .Where(p => p.Status == parsedStatus)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment?> FindByPaymentIdAsync(string paymentId)
        => await _context.Payments.FindAsync(paymentId);

    public async Task<Payment?> FindByTransactionIdAsync(string transactionId)
        => await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

    public async Task<decimal> SumAmountByUserIdAsync(int userId)
        => await _context.Payments
            .Where(p => p.UserId == userId && p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount);

    public async Task<IList<Payment>> FindByPaidAtBetweenAsync(DateTime start, DateTime end)
        => await _context.Payments
            .Where(p => p.PaidAt.HasValue
                && p.PaidAt.Value >= start
                && p.PaidAt.Value <= end
                && p.Status == PaymentStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}
