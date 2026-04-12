using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class NotificationRepository(DataContext context) : INotificationRepository
    {
        public async Task<List<Notification>> ReturnLatestUserNotifications(UserBase user)
        {
            var latestNotifications = new List<Notification>();

            if(user.Role == UserRole.Trainer)
            {
                latestNotifications = await context.Notification.OrderByDescending(n => n.SentAt)
                    .Where(n => n.TrainerId == user.Id && n.Audience == NotificationAudience.Trainer)
                    .Take(10).ToListAsync();
            }
            else if(user.Role == UserRole.Client)
            {
                latestNotifications = await context.Notification.OrderByDescending(n => n.SentAt)
                    .Where(n => n.ClientId == user.Id && n.Audience == NotificationAudience.Client)
                    .Take(10).ToListAsync();
            }
            
            return latestNotifications;
        }

        public async Task AddNotificationAsync(int trainerId, int? clientId, string message, NotificationType reminderType, CommunicationType sentThrough, NotificationAudience audience)
        {
            var newNotification = new Notification
            {
                TrainerId = trainerId,
                ClientId = clientId ?? null,
                Message = message,
                ReminderType = reminderType,
                SentThrough = sentThrough,
                SentAt = DateTime.UtcNow,
                Audience = audience
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
