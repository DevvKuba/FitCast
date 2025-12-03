using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class PaymentRepository(DataContext context, IMapper mapper) : IPaymentRepository
    {
        public async Task<List<Payment>> GetAllPaymentsForTrainerAsync(Trainer trainer)
        {
            var payments = await context.Payments.Where(p => p.TrainerId == trainer.Id)
                .OrderBy(p => p.Confirmed)
                .ThenByDescending(p => p.PaymentDate)
                .ToListAsync();
            return payments;
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            var payment = await context.Payments.Where(p => p.Id == id).FirstOrDefaultAsync();
            return payment;
        }

        public async Task<Payment?> GetPaymentWithClientByIdAsync(int id)
        {
            var payment = await context.Payments
                .Where(p => p.Id == id)
                .Include(p => p.Client)
                .FirstOrDefaultAsync();
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

        public void UpdatePaymentDetails(Payment payment, PaymentUpdateRequestDto newPaymentInfo)
        {
            PaymentUpdateDto paymentUpdateInfo = new PaymentUpdateDto
            {
                Amount = newPaymentInfo.Amount,
                Currency = newPaymentInfo.Currency,
                NumberOfSessions = newPaymentInfo.NumberOfSessions,
                PaymentDate = DateOnly.Parse(newPaymentInfo.PaymentDate),
                Confirmed = newPaymentInfo.Confirmed,
            };
            mapper.Map(paymentUpdateInfo, payment);
        }

        public async Task<decimal> CalculateClientTotalLifetimeValueAsync(Client client, DateOnly tillDate)
        {
            var confirmedClientPayments = await context.Payments
                .Where(p => p.ClientId == client.Id && p.Confirmed == true).ToListAsync();

            decimal totalValue = confirmedClientPayments.Select(p => p.Amount).Sum();
            return totalValue;
        }



        public async Task AddNewPaymentAsync(Trainer trainer, Client client, int numberOfSessions, decimal blockPrice, DateOnly paymentDate, bool? confirmed)
        {
            var payment = new Payment
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Currency = trainer.DefaultCurrency ?? "£",
                Amount = blockPrice,
                NumberOfSessions = numberOfSessions,
                PaymentDate = paymentDate,
                Confirmed = confirmed ?? false
                

            };
            await context.Payments.AddAsync(payment);
        }

        public void DeletePayment(Payment payment)
        {
            context.Remove(payment);
        }

    }
}
