using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    [DisallowConcurrentExecution]
    public class DailyTrainerWorkoutRetrieval(IServiceScopeFactory scopeFactory, ILogger<DailyTrainerWorkoutRetrieval> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            List<Trainer> trainers;

            // Get the list, then CLOSE the scope
            using (var scope = scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                trainers = await unitOfWork.TrainerRepository.GetTrainersWithAutoRetrievalAsync();
            }
            // Now 'trainers' is just a plain List<Trainer> in memory
            // No DbContext is tracking it anymore

            int totalRetrievedSessions = 0;

            logger.LogInformation("DailyTrainerWorkoutRetrieval process STARTING at: {Date}", DateTime.UtcNow);


            if (trainers.Count == 0)
            {
                logger.LogWarning("DailyTrainerWorkoutRetrieval did NOT detect any trainers with workout auto retrieval enabled, at: {Date}", DateTime.UtcNow);
                return;
            }

            foreach (Trainer trainer in trainers)
            {
                // Each trainer gets a COMPLETELY isolated scope
                using var trainerScope = scopeFactory.CreateScope();

                try 
                {
                    var trainerSyncService = trainerScope.ServiceProvider.GetRequiredService<ISessionSyncService>();
                    var notificationService = trainerScope.ServiceProvider.GetRequiredService<INotificationService>();
                    var workoutCount = await trainerSyncService.SyncSessionsAsync(trainer);
                    await notificationService.SendTrainerAutoWorkoutCollectionNoticeAsync(trainer, workoutCount, DateTime.UtcNow);

                    logger.LogDebug("Retrieved {WorkoutCount} client workouts for trainer: {TrainerName} at {Date}", workoutCount, trainer.FirstName, DateTime.UtcNow);
                    totalRetrievedSessions += workoutCount;
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Failed to sync {TrainerName}", trainer.FirstName);
                }

            }

            logger.LogInformation("DailyTrainerWOrkoutRetrieval process FINISHED, processed: {TrainerCount} trainers and {ClientSessions} of their client sessions at: {Date}", trainers.Count,totalRetrievedSessions, DateTime.UtcNow);
        }
    }
}
