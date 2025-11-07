using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class NotificationService : INotificationService
    {
        public Task SendClientBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            throw new NotImplementedException();
        }

        public Task SendTrainerBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            throw new NotImplementedException();
        }
    }
}
