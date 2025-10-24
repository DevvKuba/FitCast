using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService(IUnitOfWork unitOfWork, ISessionDataParser hevyParser, IMessageService messageService) : ISessionSyncService
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

                    var existingWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(client.Name, workout.SessionDate);
                    // if workout is not a duplicate / not yet added
                    if (existingWorkout == null)
                    {
                        unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                        await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                        await unitOfWork.Complete();

                        // indicating that their block is finished
                        if (client.CurrentBlockSession == client.TotalBlockSessions)
                        {
                            messageService.SendClientBlockCompletionReminder(client.Name);
                        }
                    }
                }
                else
                {
                    // client doesn't exist in this case so needs to be added first
                    // look over trainerId declaration
                    await unitOfWork.ClientRepository.AddNewClientAsync(clientName, null, 0);
                    await unitOfWork.Complete();

                    var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
                    unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                    await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                    await unitOfWork.Complete();
                }
            }
            return true;
        }

    }
}
