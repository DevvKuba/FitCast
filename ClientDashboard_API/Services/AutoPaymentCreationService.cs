using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class AutoPaymentCreationService(IUnitOfWork unitOfWork) : IAutoPaymentCreationService
    {
        // goal is to create a 'Pending' Trainer payment using specific information from trainer and client
        // trainer: trainerId, amount (calculated from trainer session rate * numberOfSessions, Currency (chosen by trainer)

        //client: clientId, NumberOfSessions (current block)
        public async Task CreatePendingPaymentAsync(Trainer trainer, Client client)
        {
            var blockPrice = client.TotalBlockSessions * trainer.AverageSessionPrice;
            await unitOfWork.PaymentRepository.AddNewPaymentAsync(trainer, client, client.TotalBlockSessions ?? 0, blockPrice ?? 0m, DateOnly.Parse(DateTime.Now.ToString()), false);
        }
    }
}
