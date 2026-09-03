using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class NotificationRecipientStatusRepository(DataContext context) : INotificationRecipientStatusRepository
    {
        public async Task<List<Notification>> GetExpiredUnreadNotificationsWithRecipientStatusesAsync(int userId)
        {
            var unreadExpiredStatuses = await context.NotificationRecipientStatuses
                .Where(s => s.UserId == userId &&
                !s.IsRead &&
                s.Notification.SentAt.AddDays(14) <= DateTime.UtcNow)
                .Include(s => s.Notification)
                .ToListAsync();

            List<Notification> expiredNotifications = [];

            foreach (var status in unreadExpiredStatuses)
            {
                expiredNotifications.Add(status.Notification);
            }

            return expiredNotifications;
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

        public async Task MarkNotificationsAsReadAsync(int userId, List<int> notificationIds)
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
