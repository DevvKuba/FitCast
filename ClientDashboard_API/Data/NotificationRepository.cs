using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ClientDashboard_API.Data
{
    public class NotificationRepository(DataContext context) : INotificationRepository
    {
        public async Task<List<NotificationResponseDto>> ReturnAllUserNotificationDtosAsync(UserBase user)
        {
            return await BuildUserNotificationQuery(user).ToListAsync();
        }

        public async Task<List<NotificationResponseDto>> ReturnLatestUserNotificationDtosAsync(UserBase user)
        {
            return await BuildUserNotificationQuery(user).Take(10).ToListAsync();

        }

        public async Task<List<Notification>> ReturnAllTrainerNotificationsAsync(Trainer trainer)
        {
            return await context.Notification.Where(n => n.TrainerId == trainer.Id).ToListAsync();
        }

        public IQueryable<NotificationResponseDto> BuildUserNotificationQuery(UserBase user)
        {
            var query = user.Role == UserRole.Trainer
                ? context.Notification.Where(n => n.TrainerId == user.Id && n.Audience == NotificationAudience.Trainer)
                : context.Notification.Where(n => n.ClientId == user.Id && n.Audience == NotificationAudience.Client);

            return query.OrderByDescending(n => n.SentAt).Select(n =>
                new NotificationResponseDto
                {
                    Id = n.Id,
                    TrainerId = n.TrainerId != null ? n.TrainerId : null,
                    ClientId = n.ClientId != null ? n.ClientId : null,
                    Message = n.Message,
                    ReminderType = n.ReminderType,
                    SentThrough = n.SentThrough,
                    Audience = n.Audience,
                    SentAt = n.SentAt,
                    IsRead = n.RecipientStatuses.First(s => s.UserId == user.Id).IsRead

                });
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

        public void DeleteNotifications(List<Notification> notifications)
        {
            foreach (var n in notifications)
            {
                context.Notification.Remove(n);
            }
        }
    }
}
