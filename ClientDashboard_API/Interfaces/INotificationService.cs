using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponseDto<string>> SendTrainerReminderAsync(int trainerId, int clientId);

        Task<ApiResponseDto<string>> SendClientReminderAsync(int trainerId, int clientId);

        Task<ApiResponseDto<string>> SendTrainerPendingPaymentAlertAsync(int trainerId, int clientId);
    }
}
