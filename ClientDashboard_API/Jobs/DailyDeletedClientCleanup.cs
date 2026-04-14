using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyDeletedClientCleanup(IUnitOfWork unitOfWork, ILogger<DailyDeletedClientCleanup> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyDeletedClientCleanup job STARTED at {StartTime} UTC", DateTime.UtcNow);

            var cutoffDate = DateTime.UtcNow.AddMonths(-3);
            var clientsToDelete = await unitOfWork.ClientRepository.GetSoftDeletedClientsOlderThanAsync(cutoffDate);

            if (clientsToDelete.Count == 0)
            {
                logger.LogInformation("No soft-deleted clients eligible for removal at {Time} UTC", DateTime.UtcNow);
                return;
            }

            foreach (Client client in clientsToDelete)
            {
                unitOfWork.ClientRepository.RemoveClient(client);
            }

            await unitOfWork.Complete();

            logger.LogInformation("DailyDeletedClientCleanup job FINISHED at {EndTime} UTC." +
                " Removed {RemovedCount} clients soft-deleted before {CutoffDate} UTC",
                DateTime.UtcNow, clientsToDelete.Count, cutoffDate);
        }
    }
}
