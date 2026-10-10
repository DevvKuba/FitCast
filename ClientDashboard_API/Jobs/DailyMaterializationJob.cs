using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyMaterializationJob(
        IUnitOfWork unitOfWork,
        IBookingMaterializationService materializationService,
        ILogger<DailyMaterializationJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            // start info log

            var bookingSeries = await unitOfWork.BookingSeriesRepository.GetAllBookingSeriesAsync();

            foreach(var series in bookingSeries)
            {

            }

            await unitOfWork.Complete();
        }
    }
}
