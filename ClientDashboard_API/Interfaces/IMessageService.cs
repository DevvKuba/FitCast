using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IMessageService
    {
        void PipelineClientBlockCompletionReminder(string clientName);

        void InitialiseBaseTwillioClient();

        void SendMessage(Trainer? trainer, Client? client, string senderPhoneNumber, string notificationMessage);
    }
}
