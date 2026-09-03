using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface INotificationRecipientStatusRepository
    {
        Task<List<int>> GetExpiredUnreadNotificationIdsAsync();

        Task<int> GetUnreadUserNotificationCountAsync(UserBase user);

        Task MarkNotificationsAsReadAsync(List<int> notificationIds);

        Task MarkUserNotificationsAsReadAsync(int userId, List<int> notificationIds);

    }
}
