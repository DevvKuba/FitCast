using ClientDashboard_API.Interfaces;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ClientDashboard_API.Services
{
    public class TwillioMessageService() : IMessageService
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

        public Task TrainerBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            throw new NotImplementedException();
        }

        public Task ClientBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            throw new NotImplementedException();
        }

    }
}
