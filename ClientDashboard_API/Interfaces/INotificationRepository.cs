using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> ReturnLatestTrainerNotifications(UserBase trainer);

        Task<List<Notification>> ReturnLatestClientNotifications(UserBase client);

        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough);
    }
}
