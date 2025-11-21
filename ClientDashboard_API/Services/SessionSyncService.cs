using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService(IUnitOfWork unitOfWork, ISessionDataParser hevyParser, IMessageService messageService, INotificationService notificationService) : ISessionSyncService
    {
        // PIPELINE only task currently - need to adjust to even remove in the future
        public async Task<bool> SyncDailyPipelineSessionsAsync()
        {
            // gathers all the data 
            var dailyWorkouts = await hevyParser.CallApiThroughPipelineAsync();

            if (dailyWorkouts.Count == 0) return false;

            foreach (var workout in dailyWorkouts)
            {
                string clientName = workout.Title.Split(' ')[0];
                if (await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName))
                {
                    var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

                    var existingWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(client.FirstName, workout.SessionDate);
                    // if workout is not a duplicate / not yet added
                    if (existingWorkout == null)
                    {
                        unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                        await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                        await unitOfWork.Complete();

                        // indicating that their block is finished
                        if (client.CurrentBlockSession == client.TotalBlockSessions)
                        {
                            messageService.PipelineClientBlockCompletionReminder(client.FirstName);
                        }
                    }
                }
                else
                {
                    // client doesn't exist in this case so needs to be added first
                    // look over trainerId declaration
                    await unitOfWork.ClientRepository.AddNewClientAsync(clientName, null, null);
                    await unitOfWork.Complete();

                    var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
                    unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                    await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                    await unitOfWork.Complete();
                }
            }
            return true;
        }

        public async Task<int> SyncSessionsAsync(Trainer trainer)
        {
            // finds the trainer get object
            var dailyWorkouts = await hevyParser.CallApiForTrainerAsync(trainer);
            int duplicateCount = 0;
            // look through workouts and do identical functionality compared to above methods
            // just calling different messageService methods NOT pipeline ones

            foreach (var workout in dailyWorkouts)
            {
                string clientName = workout.Title.Split(' ')[0];
                var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

                if (await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName))
                {

                    var existingWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(client.FirstName, workout.SessionDate);
                    // if workout is not a duplicate / not yet added
                    if (existingWorkout == null)
                    {
                        unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                        await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount);
                        await unitOfWork.Complete();

                        // indicating that their block is finished
                        if (client.CurrentBlockSession == client.TotalBlockSessions)
                        {
                            await notificationService.SendTrainerReminderAsync(trainer.Id, client.Id);
                        }
                    }
                    else
                    {
                        duplicateCount++;
                    }
                }
                else
                {
                    var newClient = await unitOfWork.ClientRepository.AddNewClientAsync(clientName, null, trainer.Id);
                    await unitOfWork.Complete();

                    unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(newClient);
                    await unitOfWork.WorkoutRepository.AddWorkoutAsync(newClient, workout.Title, workout.SessionDate, workout.ExerciseCount);
                    await unitOfWork.Complete();
                }
            }
            return dailyWorkouts.Count() - duplicateCount;
        }
    }
}
