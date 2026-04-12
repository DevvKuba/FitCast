using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyInvisiblePaymentCleanup(IUnitOfWork unitOfWork, ILogger<DailyDeletedClientCleanup> logger) : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            // runs on all trainer clients

            // gather every payment that is toggled to not visible 

            // comapare timeframe to filter ones that need to be deleted

            // iterate over payment list and remove from database

            // save changes 
        }
    }
}
