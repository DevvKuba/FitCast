using ClientDashboard_API.Interfaces;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ClientDashboard_API.Services
{
    public class TwillioMessageService : IMessageService
    {
        public void SendClientBlockCompletionReminder(string clientName)
        {
            var accountSid = Environment.GetEnvironmentVariable("accountSid");
            var authToken = Environment.GetEnvironmentVariable("authToken");
            var senderPhoneNumber = Environment.GetEnvironmentVariable("senderPhoneNumber");
            var receiverPhoneNumber = Environment.GetEnvironmentVariable("receiverPhoneNumber");

            Twilio.TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(
              new PhoneNumber(receiverPhoneNumber));
            messageOptions.From = new PhoneNumber(senderPhoneNumber);
            messageOptions.Body = $"{clientName}'s monthly sessions have come to an end,\n" +
                $"remember to message them in regards of a new payment.";

            var message = MessageResource.Create(messageOptions);
        }
    }
}
