using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<List<NotificationResponseDto>> ReturnAllUserNotificationDtosAsync(UserBase user);

        Task<List<NotificationResponseDto>> ReturnLatestUserNotificationDtosAsync(UserBase user);

        Task<List<Notification>> ReturnAllTrainerNotificationsAsync(Trainer trainer);

        IQueryable<NotificationResponseDto> BuildUserNotificationQuery(UserBase user);

        Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough, NotificationAudience audience);

        void DeleteNotification(Notification notification);

        void DeleteNotifications(List<Notification> notifications);
    }
}
