using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> ReturnLatestNotifications(Trainer trainer);

        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough);
    }
}
