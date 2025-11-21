using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class PaymentRepository(DataContext context) : IPaymentRepository
    {
        public async Task<List<Payment>> GetAllPaymentsForTrainerAsync(Trainer trainer)
        {
            var payments = await context.Payments.Where(p => p.TrainerId == trainer.Id).ToListAsync();
            return payments;
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            var payment = await context.Payments.Where(p => p.Id == id).FirstOrDefaultAsync();
            return payment;
        }

        public async Task<Payment?> GetPaymentWithRelatedEntitiesById(int id)
        {
            var payment = await context.Payments.Where(p => p.Id == id)
                .Include(p => p.Trainer)
                .Include(p => p.Client)
                .FirstOrDefaultAsync();

            return payment;
        }

        public async Task AddNewPaymentAsync(Trainer trainer, Client client, int numberOfSessions, decimal blockPrice, DateOnly paymentDate)
        {
            var payment = new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Currency = trainer.DefaultCurrency ?? "£",
                Amount = blockPrice,
                NumberOfSessions = numberOfSessions,
                PaymentDate = paymentDate

            };
            await context.Payments.AddAsync(payment);
        }

        public void DeletePayment(Payment payment)
        {
            context.Remove(payment);
        }

    }
}
