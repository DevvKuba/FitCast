using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyExpiredTokenCleanup(IUnitOfWork unitOfWork, ILogger<DailyExpiredTokenCleanup> logger ) : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            // log job starting

            // gather all expired email verification tokens
            // then all password reset tokens

            // call a method as part of each corresponding repository to run a loop 
            // to remove them all systemetically (mark as deleted via EF core)

            // save changes
        }
    }
}
