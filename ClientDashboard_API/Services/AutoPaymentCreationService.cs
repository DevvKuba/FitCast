using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class AutoPaymentCreationService(IUnitOfWork unitOfWork) : IAutoPaymentCreationService
    {
        public async Task CreatePendingPaymentAsync(Trainer trainer, Client client)
        {
            var blockPrice = client.TotalBlockSessions * trainer.AverageSessionPrice;
            await unitOfWork.PaymentRepository.AddNewPaymentAsync(trainer, client, client.TotalBlockSessions ?? 0, blockPrice ?? 0m, DateOnly.FromDateTime(DateTime.Now), false);
        }
    }
}
