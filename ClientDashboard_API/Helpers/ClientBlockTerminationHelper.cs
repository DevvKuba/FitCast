using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;

namespace ClientDashboard_API.Helpers
{
    public class ClientBlockTerminationHelper(INotificationService notificationService, IAutoPaymentCreationService autoPaymentService) : IClientBlockTerminationHelper
    {
        public async Task<ApiResponseDto<string>> CreateAllAdequateEntityReminderAsync(Client client)
        {
            if (client.Trainer is not null)
            {
                await notificationService.SendTrainerBlockReminderAsync((int)client.TrainerId!, client.Id);

                await notificationService.SendClientBlockReminderAsync((int)client.TrainerId!, client.Id);


                if (client.Trainer.AutoPaymentSetting)
                {
                   await autoPaymentService.CreatePendingPaymentAsync(client.Trainer, client);

                   await notificationService.SendTrainerPendingPaymentAlertAsync(client.Trainer.Id, client.Id);

                  // notification to client around how much they are due 
                }
            }
            return new ApiResponseDto<string> { Data = null, Message = "process finalised without any processing errors", Success = true};
        }


    }
}
