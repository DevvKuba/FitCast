using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IMessageService
    {
        void PipelineClientBlockCompletionReminder(string clientName);

        void InitialiseBaseTwillioClient();

        void SendMessage(Trainer? trainer, Entities.Client? client, string senderPhoneNumber, string notificationMessage);
    }
}
