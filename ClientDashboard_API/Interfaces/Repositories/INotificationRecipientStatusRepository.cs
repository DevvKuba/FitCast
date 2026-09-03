using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface INotificationRecipientStatusRepository
    {
        Task<List<Notification>> GetExpiredUnreadNotificationsAsync();

        Task<int> GetUnreadUserNotificationCountAsync(UserBase user);

        Task MarkNotificationsAsReadAsync(int userId, List<int> notificationIds);

    }
}
