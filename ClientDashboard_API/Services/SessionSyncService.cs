using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using System.Diagnostics.Eventing.Reader;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService(IUnitOfWork unitOfWork, ISessionDataParser hevyParser, IMessageService messageService,
        INotificationService notificationService, IAutoPaymentCreationService autoPaymentService) : ISessionSyncService
    {
        public async Task<int> SyncSessionsAsync(Trainer trainer)
        {
            // finds the trainer get object
            var dailyWorkouts = await hevyParser.CallApiForTrainerAsync(trainer);
            int duplicateOrExcludedCount = 0;
            // look through workouts and do identical functionality compared to above methods
            // just calling different messageService methods NOT pipeline ones

            foreach (var workout in dailyWorkouts)
            {
                string clientName = workout.Title.Split(' ')[0];

                if (!trainer.ExcludedNames.Contains(clientName.ToLower()))
                {
                    // this client name will always be retrived during syncing
                    var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

                    if (await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName))
                    {

                        var existingWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateByIdAsync(client!.Id, workout.SessionDate);
                        // if workout is not a duplicate / not yet added
                        if (existingWorkout == null)
                        {
                            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workout.Title, workout.SessionDate, workout.ExerciseCount, workout.Duration);
                            await unitOfWork.Complete();

                            // indicating that their block is finished
                            if (client.CurrentBlockSession == client.TotalBlockSessions)
                            {
                                if (trainer.NotificationsEnabled)
                                {
                                    await notificationService.SendTrainerReminderAsync(trainer.Id, client.Id);
                                }
                                if (trainer.AutoPaymentSetting)
                                {
                                    await autoPaymentService.CreatePendingPaymentAsync(trainer, client);
                                    await unitOfWork.Complete();
                                }
                            }
                        }
                        else
                        {
                            duplicateOrExcludedCount++;
                        }
                    }

                    else
                    {
                        var newClient = await unitOfWork.ClientRepository.AddNewClientUnderTrainerAsync(clientName, null, null, trainer.Id);
                        await unitOfWork.Complete();

                        unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(newClient!);
                        await unitOfWork.WorkoutRepository.AddWorkoutAsync(newClient!, workout.Title, workout.SessionDate, workout.ExerciseCount, workout.Duration);
                        await unitOfWork.Complete();
                    }
                }
                else
                {
                    duplicateOrExcludedCount++;
                }
            }
            return dailyWorkouts.Count() - duplicateOrExcludedCount;
        }
    }
}
