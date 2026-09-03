using ClientDashboard_API.Interfaces.Repositories;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyNotificationStatusJob(ILogger<DailyNotificationStatusJob> logger, IUnitOfWork unitOfWork) : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            // gather notifications that are unread & 14 days or older (expired)

            // if there are any change their statusses

            // save changes + log final
        }
    }
}
