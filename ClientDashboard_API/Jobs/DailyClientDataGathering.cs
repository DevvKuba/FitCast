using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyClientDataGathering(IUnitOfWork unitOfWork, IClientDailyFeatureService dailyService) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var trainers = await unitOfWork.TrainerRepository.GetAllTrainersAsync();

            foreach(Trainer trainer in trainers)
            {
                var trainerClients = await unitOfWork.TrainerRepository.GetTrainerClientsAsync(trainer);
                
                foreach (Client client in trainerClients)
                {
                    await dailyService.ExecuteClientDailyGatheringAsync(client);

                    client.DailySteps = 0;
                }

                Console.WriteLine($"Gathered Daily Data for: {trainerClients.Count} clients under trainer: {trainer.FirstName} at: {DateTime.UtcNow}");
            }

            await unitOfWork.Complete();
        }
    }
}
