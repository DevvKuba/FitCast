using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Api.V2010.Account.Usage.Record;

namespace ClientDashboard_API.Data
{
    public class NotificationRepository(DataContext context) : INotificationRepository
    {
        public async Task<List<Notification>> ReturnUnreadTrainerNotifications(UserBase user)
        {
            var latestNotifications = await context.Notification
                .OrderByDescending(n => n.SentAt)
                .Where(n => n.TrainerId == user.Id && n.IsRead == false)
                .ToListAsync();
            return latestNotifications;
        }

        public async Task<List<Notification>> ReturnUnreadClientNotifications(UserBase user)
        {
            var latestNotifications = await context.Notification
                .OrderByDescending(n => n.SentAt)
                .Where(n => n.ClientId == user.Id && n.IsRead == false)
                .ToListAsync();
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
            await context.Notification.AddAsync(newNotification);
        }

        public void DeleteNotification(Notification notification)
        {
            context.Notification.Remove(notification);
        }
    }
}
