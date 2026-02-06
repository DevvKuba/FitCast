using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;

namespace ClientDashboard_API.Helpers
{
    public class ClientBlockTerminationHelper(IUnitOfWork unitOfWork, INotificationService notificationService, IAutoPaymentCreationService autoPaymentService) : IClientBlockTerminationHelper
    {
        public async Task CreateAdequateRemindersAndPaymetsAsync(Client client)
        {
            if (client.Trainer is not null)
            {
                await notificationService.SendTrainerReminderAsync((int)client.TrainerId!, client.Id);

                if (client.Trainer.AutoPaymentSetting)
                {
                    await autoPaymentService.CreatePendingPaymentAsync(client.Trainer, client);
                    await notificationService.SendTrainerPendingPaymentAlertAsync(client.Trainer.Id, client.Id);
                    await unitOfWork.Complete();
                }
            }
        }
    }
}
