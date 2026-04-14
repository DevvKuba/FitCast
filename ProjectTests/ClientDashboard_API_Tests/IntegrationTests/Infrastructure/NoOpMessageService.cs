using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API_Tests.IntegrationTests.Infrastructure
{
    public class NoOpMessageService : IMessageService
    {
        public void InitialiseBaseTwillioClient()
        {
        }

        public void PipelineClientBlockCompletionReminder(string clientName)
        {
        }

        public void SendSMSMessage(Trainer? trainer, Client? client, string senderPhoneNumber, string notificationMessage)
        {
        }
    }
}
