using ClientDashboard_API.Interfaces.Repositories;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyMaterializationJob(IUnitOfWork unitOfWork, ILogger<DailyMaterializationJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            // start info log

            var bookingSeries = await unitOfWork.BookingSeriesRepository.GetAllBookingSeriesAsync();

            var todaysDate = DateTime.UtcNow;

            var cutOffDate = todaysDate.AddDays(90);

            foreach(var series in bookingSeries)
            {

            }

            // check reoccurance.. 
            // FOR that BookingSeries create appropriate BookedSessionSlots
            // could be a sub method here or loop
            // MaterializedUntil = cutOffDate

            await unitOfWork.Complete();
        }
    }
}
