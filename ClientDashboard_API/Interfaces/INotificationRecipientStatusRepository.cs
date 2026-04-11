using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRecipientStatusRepository
    {
        Task<NotificationRecipientStatus> GetNotificationRecipientStatusByIdAsync(int statusId);
        
        Task<int> GetUnreadUserNotificationCountAsync(int userId);

        Task<List<NotificationRecipientStatus>> GetLatestUserNotificationStatusesAsync(int userId);

        Task MarkNotificationsAsReadAsync(int userId, List<int> notificationIds);

        Task AddNotificationRecipientStatusAsync(int userId, int notificationId);

        Task AddNotificationRecipientStatusesAsync(List<NotificationRecipientStatus> recipientStatuses);

        void DeleteNotificationRecipientStatus(NotificationRecipientStatus recipientStatus);
    }
}
