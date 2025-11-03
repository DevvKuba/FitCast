using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface INotificationRepository
    {
        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, string reminderType, string sentThrough);
    }
}
