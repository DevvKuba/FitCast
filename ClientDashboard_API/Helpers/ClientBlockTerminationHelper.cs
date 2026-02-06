using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;

namespace ClientDashboard_API.Helpers
{
    public class ClientBlockTerminationHelper(INotificationService notificationService, IAutoPaymentCreationService autoPaymentService) : IClientBlockTerminationHelper
    {
        public async Task<ApiResponseDto<string>> CreateAdequateTrainerRemindersAndPaymentsAsync(Client client)
        {
            ApiResponseDto<string> response;

            if (client.Trainer is not null)
            {
                response = await notificationService.SendTrainerReminderAsync((int)client.TrainerId!, client.Id);

                if (!response.Success)
                {
                    return new ApiResponseDto<string> { Data = null, Message = $"Client workout added however notification was not created" , Success = false};
                }

                if (client.Trainer.AutoPaymentSetting)
                {
                    await autoPaymentService.CreatePendingPaymentAsync(client.Trainer, client);
                    response = await notificationService.SendTrainerPendingPaymentAlertAsync(client.Trainer.Id, client.Id);

                    if (!response.Success)
                    {
                        return new ApiResponseDto<string> { Data = null, Message = $"Client workout added however pending payment was not created", Success = false };
                    }
                }
            }
            return new ApiResponseDto<string> { Data = null, Message = "process finalised without any processing errors", Success = true};
        }
    }
}
