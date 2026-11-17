using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    [DisallowConcurrentExecution]
    public class DailySessionSyncJob(IServiceScopeFactory scopeFactory, ILogger<DailySessionSyncJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            List<Trainer> trainers;

            // Get the list, then CLOSE the scope
            using (var scope = scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                trainers = await unitOfWork.TrainerRepository.GetAllTrainersAsync();
            }

            int totalSyncedSessions = 0;

            logger.LogInformation("DailySessionSyncJob process STARTING at: {Date}", DateTime.UtcNow);

            if (trainers.Count == 0)
            {
                logger.LogWarning("DailySessionSyncJob did NOT find any trainers at: {Date}", DateTime.UtcNow);
                return;
            }

            foreach (Trainer trainer in trainers)
            {
                // Each trainer gets a COMPLETELY isolated scope
                using var trainerScope = scopeFactory.CreateScope();

                try
                {
                    var sessionSyncService = trainerScope.ServiceProvider.GetRequiredService<ISessionSyncService>();
                    var notificationService = trainerScope.ServiceProvider.GetRequiredService<INotificationService>();
                    var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

                    if (syncedCount > 0)
                    {
                        await notificationService.SendTrainerDailySessionSyncNoticeAsync(trainer, syncedCount, DateTime.UtcNow);
                    }

                    logger.LogDebug("Synced {SyncedCount} sessions for trainer: {TrainerName} at {Date}", syncedCount, trainer.FirstName, DateTime.UtcNow);
                    totalSyncedSessions += syncedCount;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to sync sessions for {TrainerName}", trainer.FirstName);
                }
            }

            logger.LogInformation("DailySessionSyncJob process FINISHED, processed: {TrainerCount} trainers" +
                " and synced {SyncedSessions} of their sessions at: {Date}", trainers.Count, totalSyncedSessions, DateTime.UtcNow);
        }
    }
}
