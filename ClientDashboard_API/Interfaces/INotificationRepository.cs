using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRepository
    {
        Task<int> ReturnUnreadTrainerNotificationCount(UserBase trainer);

        Task<int> ReturnUnreadClientNotificationCount(UserBase client);

        Task MarkNotificationsAsRead(List<Notification> notificationList);

        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough);
    }
}
