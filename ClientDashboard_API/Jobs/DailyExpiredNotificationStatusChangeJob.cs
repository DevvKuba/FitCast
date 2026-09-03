using ClientDashboard_API.Interfaces.Repositories;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyExpiredNotificationStatusChangeJob(ILogger<DailyExpiredNotificationStatusChangeJob> logger, IUnitOfWork unitOfWork) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyExpiredNotificationStatusChangeJob job STARTED at {StartTime} UTC", DateTime.UtcNow);

            var expiredNotificationIds = await unitOfWork.NotificationRecipientStatusRepository.GetExpiredUnreadNotificationIdsAsync();

            if (expiredNotificationIds.Count == 0)
            {
                logger.LogInformation("No expired unread notifications found at {Time} UTC", DateTime.UtcNow);
                return;
            }

            await unitOfWork.NotificationRecipientStatusRepository.MarkNotificationsAsReadAsync(expiredNotificationIds);

            await unitOfWork.Complete();

            logger.LogInformation("DailyExpiredNotificationStatusChangeJob job FINISHED at {EndTime} UTC. Marked {MarkedCount} expired notifications as read",
                DateTime.UtcNow, expiredNotificationIds.Count);
        }
    }
}
