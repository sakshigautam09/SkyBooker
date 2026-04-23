using SkyBooker.PaymentService.Entities;

namespace SkyBooker.PaymentService.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> FindByBookingIdAsync(string bookingId);
    Task<IList<Payment>> FindByUserIdAsync(int userId);
    Task<IList<Payment>> FindByStatusAsync(string status);
    Task<Payment?> FindByPaymentIdAsync(string paymentId);
    Task<Payment?> FindByTransactionIdAsync(string transactionId);
    Task<decimal> SumAmountByUserIdAsync(int userId);
    Task<IList<Payment>> FindByPaidAtBetweenAsync(DateTime start, DateTime end);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
}
