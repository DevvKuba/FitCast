using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService(IUnitOfWork unitOfWork, ISessionDataParser hevyParser) : ISessionSyncService
    {
        public async Task<bool> SyncDailySessions()
        {
            // gathers all the data 
            var dailyWorkouts = await hevyParser.CallApi();

            if (dailyWorkouts.Count == 0) return false;

            foreach (var workout in dailyWorkouts)
            {
                string clientName = workout.Title.Split(' ')[0];
                if (await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName))
                {
                    var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
                    unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                    await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                }
                else
                {
                    await unitOfWork.ClientRepository.AddNewClientAsync(clientName, null);
                }
                await unitOfWork.Complete();
            }
            return true;
        }

    }
}
