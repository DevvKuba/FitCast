using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;

namespace ClientDashboard_API.Services
{
    public class SessionSyncService(IUnitOfWork unitOfWork, IClientBlockTerminationHelper clientBlockTerminator, TimeProvider timeProvider) : ISessionSyncService
    {
        public async Task<int> SyncSessionsAsync(Trainer trainer)
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var finishedSlots = await unitOfWork.BookedSessionSlotRepository.GetTrainerFinishedScheduledSlotsAsync(trainer.Id, nowUtc);

            if (!trainer.CalendarSyncEnabled)
            {
                foreach (var slot in finishedSlots)
                {
                    slot.Status = SessionStatus.Completed;
                }
                await unitOfWork.Complete();
                return 0;
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(trainer.TimeZoneId);

            foreach (var slot in finishedSlots)
            {
                var client = slot.Client;
                var sessionDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(slot.StartDateTime, tz));

                unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
                await unitOfWork.WorkoutRepository.AddSyncedWorkoutAsync(client, slot, sessionDate);
                slot.Status = SessionStatus.Synced;
                await unitOfWork.Complete();

                if (client.CurrentBlockSession == client.TotalBlockSessions)
                {
                    await clientBlockTerminator.CreateAllAdequateEntityReminderAsync(client);
                }
            }

            return finishedSlots.Count;
        }
    }
}
