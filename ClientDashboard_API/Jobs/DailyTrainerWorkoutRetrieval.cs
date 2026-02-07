using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyTrainerWorkoutRetrieval(IServiceScopeFactory scopeFactory, ILogger<DailyTrainerWorkoutRetrieval> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = scopeFactory.CreateScope();

            var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var syncService = scope.ServiceProvider.GetRequiredService<SessionSyncService>(); 

            int totalRetrievedSessions = 0;

            logger.LogInformation("DailyTrainerWorkoutRetrieval process STARTING at: {Date}", DateTime.UtcNow);

            // variables for number of trainers processed and their clients 

            var trainers = await unitOfWork.TrainerRepository.GetTrainersWithAutoRetrievalAsync();

            if (trainers.Count == 0)
            {
                logger.LogWarning("DailyTrainerWorkoutRetrieval did NOT detect any trainers with workout auto retrieval enabled, at: {Date}", DateTime.UtcNow);
                return;
            }

            foreach (Trainer trainer in trainers)
            {
                var workoutCount = await syncService.SyncSessionsAsync(trainer);
                // if count was zero logging message that there were no sessions to sync
                if (workoutCount == 0)
                {
                    logger.LogWarning("Retrived no new workout for trainer: {TrainerName} at {Date}", trainer.FirstName, DateTime.UtcNow);
                    continue;
                }

                logger.LogDebug("Retrieved {WorkoutCount} client workouts for trainer: {TrainerName} at {Date}", workoutCount, trainer.FirstName, DateTime.UtcNow);
                totalRetrievedSessions += workoutCount;

            }

            logger.LogInformation("DailyTrainerWOrkoutRetrieval process FINISHED, processed: {TrainerCount} trainers and {ClientSessions} of their client sessions at: {Date}", trainers.Count,totalRetrievedSessions, DateTime.UtcNow);
        }
    }
}
