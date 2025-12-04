using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyClientDataGathering(IUnitOfWork unitOfWork, IClientDailyFeatureService dailyService, ILogger<DailyClientDataGathering> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("DailyClientDataGathering job STARTED at {StartTime} UTC", DateTime.UtcNow);
            
            var totalProcessed = 0;
            var totalFailed = 0;
            
            try
            {
                var trainers = await unitOfWork.TrainerRepository.GetAllTrainersAsync();
                
                if (trainers == null || trainers.Count == 0)
                {
                    logger.LogWarning("No trainers found to process at {Time} UTC", DateTime.UtcNow);
                    return;
                }
                
                logger.LogInformation("Found {TrainerCount} trainers to process", trainers.Count);

                foreach (Trainer trainer in trainers)
                {
                    var clientsProcessed = 0;
                    var clientsFailed = 0;

                    try
                    {
                        var trainerClients = await unitOfWork.TrainerRepository.GetTrainerClientsAsync(trainer);

                        if (trainerClients == null || trainerClients.Count == 0)
                        {
                            logger.LogInformation("No clients found for trainer {TrainerName} (ID: {TrainerId})",
                                trainer.FirstName, trainer.Id);
                            continue;
                        }

                        logger.LogInformation("Processing {ClientCount} clients for trainer {TrainerName} (ID: {TrainerId})",
                            trainerClients.Count, trainer.FirstName, trainer.Id);

                        foreach (Client client in trainerClients)
                        {
                            try
                            {
                                await dailyService.ExecuteClientDailyGatheringAsync(client);
                                client.DailySteps = 0;

                                clientsProcessed++;
                                totalProcessed++;

                                logger.LogDebug("Successfully processed client {ClientName} (ID: {ClientId}) for trainer {TrainerName}",
                                    client.FirstName, client.Id, trainer.FirstName);
                            }
                            catch (Exception ex)
                            {
                                clientsFailed++;
                                totalFailed++;

                                logger.LogError(ex,
                                    "Failed to process client {ClientName} (ID: {ClientId}) for trainer {TrainerName} (ID: {TrainerId}). Error: {ErrorMessage}",
                                    client.FirstName, client.Id, trainer.FirstName, trainer.Id, ex.Message);

                                // Continue processing other clients despite failure
                            }
                        }

                        await unitOfWork.Complete();
                        
                        logger.LogInformation(
                            "DailyClientDataGathering clients SAVED successfully for trainer: {TrainerName} at {EndTime} UTC. Total processed: {TotalProcessed}, Total failed: {TotalFailed}",
                            trainer.FirstName ,DateTime.UtcNow, totalProcessed, totalFailed);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, 
                            "Failed to retrieve or process clients for trainer {TrainerName} (ID: {TrainerId}) at {Time} UTC. Error: {ErrorMessage}", 
                            trainer.FirstName, trainer.Id, DateTime.UtcNow, ex.Message);
                        
                        // Continue with next trainer
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "DailyClientDataGathering job FAILED critically at {Time} UTC. Total processed before failure: {TotalProcessed}, Total failed: {TotalFailed}. Error: {ErrorMessage}", 
                    DateTime.UtcNow, totalProcessed, totalFailed, ex.Message);
                
                throw; // Re-throw to mark the job as failed in Quartz
            }
        }
    }
}
