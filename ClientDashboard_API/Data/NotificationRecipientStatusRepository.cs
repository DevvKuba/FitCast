using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class NotificationRecipientStatusRepository(DataContext context) : INotificationRecipientStatusRepository
    {
        public async Task<NotificationRecipientStatus> GetNotificationRecipientStatusByIdAsync(int statusId)
        {
            return await context.NotificationRecipientStatuses.Where(n => n.Id == statusId).FirstAsync();
        }
        public async Task<List<NotificationRecipientStatus>> GetLatestUserNotificationStatusesAsync(int userId)
        {
            var latestRecipientStatuses = await context.NotificationRecipientStatuses
                .Include(n => n.Notification)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Notification.SentAt)
                .Take(10)
                .ToListAsync();

            return latestRecipientStatuses;
        }

        public async Task<int> GetUnreadUserNotificationCountAsync(int userId)
        {
            var unreadNotificationCount = await context.NotificationRecipientStatuses
                .CountAsync(n => n.UserId == userId && n.IsRead == false);

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

        public async Task AddNotificationRecipientStatusAsync(int userId, int notificationId)
        {
            var recipientStatus = new NotificationRecipientStatus
            {
                UserId = userId,
                NotificationId = notificationId,
            };

            await context.NotificationRecipientStatuses.AddAsync(recipientStatus);
        }

        public async Task AddNotificationRecipientStatusesAsync(List<NotificationRecipientStatus> recipientStatuses)
        {
            await context.NotificationRecipientStatuses.AddRangeAsync(recipientStatuses);
        }

        public void DeleteNotificationRecipientStatus(NotificationRecipientStatus recipientStatus)
        {
            context.NotificationRecipientStatuses.Remove(recipientStatus);
        }
    }
}
