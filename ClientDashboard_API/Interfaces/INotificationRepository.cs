using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> ReturnUnreadTrainerNotifications(UserBase user);

        Task<List<Notification>> ReturnUnreadClientNotifications(UserBase user);

        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough);
    }
}
