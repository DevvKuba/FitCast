using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class ClientDailyDataGathering(IUnitOfWork unitOfWork, IClientDailyFeatureService dailyService) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var trainers = await unitOfWork.TrainerRepository.GetTra
        }
    }
}
