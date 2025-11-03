using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ClientDashboard_API.Services
{
    public class TwillioMessageService(IUnitOfWork unitOfWork) : IMessageService
    {

        // would need to change to id for more accurate retrieval rather than clientName
        public void PipelineClientBlockCompletionReminder(string clientName)
        {
            var ACCOUNT_SID = Environment.GetEnvironmentVariable("ACCOUNT_SID");
            var AUTH_TOKEN = Environment.GetEnvironmentVariable("AUTH_TOKEN");
            var SENDER_PHONE_NUMBER = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");
            var RECEIVER_PHONE_NUMBER = Environment.GetEnvironmentVariable("RECEIVER_PHONE_NUMBER");

            Twilio.TwilioClient.Init(ACCOUNT_SID, AUTH_TOKEN);

            var messageOptions = new CreateMessageOptions(
              new PhoneNumber(RECEIVER_PHONE_NUMBER));
            messageOptions.From = new PhoneNumber(SENDER_PHONE_NUMBER);
            messageOptions.Body = $"{clientName}'s monthly sessions have come to an end,\n" +
                $"remember to message them in regards of a new payment.";

            var message = MessageResource.Create(messageOptions);
        }


        public void InitialiseBaseTwillioClient()
        {
            //initilaise base client
            var ACCOUNT_SID = Environment.GetEnvironmentVariable("ACCOUNT_SID");
            var AUTH_TOKEN = Environment.GetEnvironmentVariable("AUTH_TOKEN");
            Twilio.TwilioClient.Init(ACCOUNT_SID, AUTH_TOKEN);
        }

        public void SendMessage(Trainer? trainer, Entities.Client? client, string senderPhoneNumber, string notificationMessage)
        {
            var messageOptions = new CreateMessageOptions(
              // TODO would be changed to recipient PhoneNumber
              new PhoneNumber(trainer.Email ?? client.Name));
            messageOptions.From = new PhoneNumber(senderPhoneNumber);
            messageOptions.Body = notificationMessage;

            var message = MessageResource.Create(messageOptions);
        }

    }
}
