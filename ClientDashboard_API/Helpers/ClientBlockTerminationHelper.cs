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
                response = await notificationService.SendTrainerBlockReminderAsync((int)client.TrainerId!, client.Id);

                if (!response.Success)
                {
                    return new ApiResponseDto<string> { Data = null, Message = $"Client workout added however notification was not created" , Success = false};
                }

                if (client.Trainer.AutoPaymentSetting)
                {
                    response = await autoPaymentService.CreatePendingPaymentAsync(client.Trainer, client);

                    if (!response.Success)
                    {
                        return new ApiResponseDto<string> { Data = null, Message = $"Client workout added however pending payment record was not created", Success = false };
                    }

                    response = await notificationService.SendTrainerPendingPaymentAlertAsync(client.Trainer.Id, client.Id);

                    if (!response.Success)
                    {
                        return new ApiResponseDto<string> { Data = null, Message = $"Client workout added however pending payment alert was not created", Success = false };
                    }
                }
            }
            return new ApiResponseDto<string> { Data = null, Message = "process finalised without any processing errors", Success = true};
        }
    }
}
