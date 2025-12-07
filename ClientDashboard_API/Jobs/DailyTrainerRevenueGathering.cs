using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyTrainerRevenueGathering(IUnitOfWork unitOfWork, ITrainerDailyRevenueService revenueService, ILogger<DailyTrainerRevenueGathering> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyTrainerRevenueGathering process STARTING at: {DateToday}", DateOnly.FromDateTime(DateTime.UtcNow));

            var eligibleTrainers = await unitOfWork.TrainerRepository.GetAllTrainersEligibleForRevenueTrackingAsync();

            logger.LogDebug("Gathered: {TrainerCount} eligible trainers for revenue data gathering", eligibleTrainers.Count);

            foreach(Trainer trainer in eligibleTrainers)
            {
                await revenueService.ExecuteTrainerDailyRevenueGatheringAsync(trainer);

                logger.LogDebug("Processed trainer: {TrainerName}'s revenue at: {DateToday}", trainer.FirstName, DateOnly.FromDateTime(DateTime.UtcNow));
            }

            logger.LogInformation("DailyTrainerRevenueGathering process has FINALISED at: {DateToday}", DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }
}
