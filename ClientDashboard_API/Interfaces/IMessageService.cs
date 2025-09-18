namespace ClientDashboard_API.Interfaces
{
    public interface IMessageService
    {
        void SendClientBlockCompletionReminder(string clientName);
    }
}
