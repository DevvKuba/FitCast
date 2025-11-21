using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetPaymentByIdAsync(int id);

        Task<Payment?> GetPaymentWithRelatedEntitiesById(int id);

        Task<List<Payment>> GetAllPaymentsForTrainerAsync(Trainer trainer);

        Task AddNewPaymentAsync(Trainer trainer, Client client, int numberOfSessions, decimal blockPrice, DateOnly paymentDate);

        void DeletePayment(Payment payment);

    }
}
