namespace ClientDashboard_API.Interfaces
{
    public interface INotificationService
    {
        Task SendTrainerBlockCompletionReminderAsync(int trainerId, int clientId);

        Task SendClientBlockCompletionReminderAsync(int trainerId, int clientId);
    }
}
