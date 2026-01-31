using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Helpers
{
    public static class NotificationMessageHelper
    {
        private static readonly Dictionary<Enums.NotificationType, Func<Trainer, Client, string>> MessageTemplates = new()
        {
            [Enums.NotificationType.TrainerBlockCompletionReminder] =
            (client, trainer) => $"{client.FirstName}'s monthly sessions have come to an end,\n" +
                                    $"remember to message them in regards of a new monthly payment.",
            [Enums.NotificationType.ClientBlockCompletionReminder] =
            (client, trainer) => $"Hey {client.FirstName}! just wanted to" +
                                    "inform you that our monthly sessions have come to an end,\n" +
                    $"If you could place a block payment before our next session block that would be great."
        };

        public static string GetMessage (Enums.NotificationType notificationType, Trainer trainer, Client client)
        {
            if (MessageTemplates.TryGetValue(notificationType, out var notificationMessage))
            {
                return notificationMessage(trainer, client);
            }

            throw new ArgumentException($"No message template found for notification type: {notificationType}");
        }
        
    }
}
