using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class NotificationRecipientStatusRepository(DataContext context) : INotificationRecipientStatusRepository
    {
        public async Task<List<int>> GetExpiredUnreadNotificationIdsAsync()
        {
            var unreadExpiredStatuses = await context.NotificationRecipientStatuses
                .Where(s => !s.IsRead &&
                s.Notification.SentAt.AddDays(14) <= DateTime.UtcNow)
                .ToListAsync();

            List<int> expiredNotificationIds = [];
            
            foreach (var status in unreadExpiredStatuses)
            {
                expiredNotificationIds.Add(status.NotificationId);
            }
            
            return expiredNotificationIds;
        }

        public async Task<int> GetUnreadUserNotificationCountAsync(UserBase user)
        {
            var expectedAudience = user.Role == UserRole.Trainer
                ? NotificationAudience.Trainer
                : NotificationAudience.Client;

            var unreadNotificationCount = await context.NotificationRecipientStatuses
                .CountAsync(n => n.UserId == user.Id && n.IsRead == false && n.Notification.Audience == expectedAudience);

            return unreadNotificationCount;
        }

        public async Task MarkNotificationsAsReadAsync(List<int> notificationIds)
        {
            var notificationsToUpdate = await context.NotificationRecipientStatuses
                .Where(n => notificationIds.Contains(n.NotificationId) && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notificationsToUpdate)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

        public async Task MarkUserNotificationsAsReadAsync(int userId, List<int> notificationIds)
        {
            var notificationsToUpdate = await context.NotificationRecipientStatuses
                .Where(n => n.UserId == userId && notificationIds.Contains(n.NotificationId) && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notificationsToUpdate)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

    }
}
