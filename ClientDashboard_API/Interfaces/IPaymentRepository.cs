using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetPaymentByIdAsync(int id);

        Task<Payment?> GetPaymentWithClientByIdAsync(int id);

        Task<Payment?> GetPaymentWithRelatedEntitiesById(int id);

        Task<List<Payment>> GetAllPaymentsForTrainerAsync(Trainer trainer);

        void UpdatePaymentDetails(Payment payment, PaymentUpdateRequestDto newPaymentInfo);

        Task AddNewPaymentAsync(Trainer trainer, Client client, int numberOfSessions, decimal blockPrice, DateOnly paymentDate, bool? confirmed);

        void DeletePayment(Payment payment);

    }
}
