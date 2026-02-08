using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponseDto<string>> SendTrainerBlockReminderAsync(int trainerId, int clientId);

        Task<ApiResponseDto<string>> SendClientBlockReminderAsync(int trainerId, int clientId);

        Task<ApiResponseDto<string>> SendTrainerPendingPaymentAlertAsync(int trainerId, int clientId);

        Task<ApiResponseDto<string>> SendTrainerAutoWorkoutCollectionNoticeAsync(int trainerId);
    }
}
