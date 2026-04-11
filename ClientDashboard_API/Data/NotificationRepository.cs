using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class NotificationRepository(DataContext context) : INotificationRepository
    {
        public async Task<List<Notification>> ReturnLatestTrainerNotifications(UserBase user)
        {
            var latestNotifications = await context.Notification.OrderByDescending(n => n.SentAt).Where(n => n.TrainerId == user.Id).Take(10).ToListAsync();
            return latestNotifications;
        }

        public async Task<List<Notification>> ReturnLatestClientNotifications(UserBase user)
        {
            var latestNotifications = await context.Notification.OrderByDescending(n => n.SentAt).Where(n => n.ClientId == user.Id).Take(10).ToListAsync();
            return latestNotifications;
        }

        public async Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough)
        {
            var newNotification = new Notification
            {
                TrainerId = trainerId,
                ClientId = clientId ?? null,
                Message = message,
                ReminderType = reminderType,
                SentThrough = sentThrough,
                SentAt = DateTime.UtcNow,
            };

            newNotification.RecipientStatuses.Add(new NotificationRecipientStatus {UserId = trainerId, IsRead = false, ReadAt = null});

            if (clientId.HasValue)
            {
                newNotification.RecipientStatuses.Add(new NotificationRecipientStatus{UserId = clientId.Value, IsRead = false, ReadAt = null});
            }

            await context.Notification.AddAsync(newNotification);
        }

        public void DeleteNotification(Notification notification)
        {
            context.Notification.Remove(notification);
        }
    }
}
