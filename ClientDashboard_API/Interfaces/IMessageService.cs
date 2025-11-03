namespace ClientDashboard_API.Interfaces
{
    public interface IMessageService
    {
        void PipelineClientBlockCompletionReminder(string clientName);

        Task TrainerBlockCompletionReminderAsync(int trainerId, int clientId);

        Task ClientBlockCompletionReminderAsync(int trainerId, int clientId);
    }
}
