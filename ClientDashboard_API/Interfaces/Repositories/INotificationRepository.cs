using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<List<NotificationResponseDto>> ReturnAllUserNotifications(UserBase user);

        Task<List<NotificationResponseDto>> ReturnLatestUserNotifications(UserBase user);

        IQueryable<NotificationResponseDto> BuildUserNotificationQuery(UserBase user);

        void DeleteNotification(Notification notification);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough, NotificationAudience audience);
    }
}
