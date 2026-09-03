using ClientDashboard_API.Interfaces.Repositories;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyNotificationStatusJob(ILogger<DailyNotificationStatusJob> logger, IUnitOfWork unitOfWork) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var expiredNotifications = await unitOfWork.NotificationRecipientStatusRepository.GetExpiredUnreadNotificationsAsync();

            // if there are any change their statusses

            // save changes + log final
        }
    }
}
