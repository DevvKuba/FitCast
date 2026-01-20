using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public int? TrainerId { get; set; }

        public int? ClientId { get; set; }

        public required string Message { get; set; }

        public required NotificationType ReminderType { get; set; }

        public required CommunicationType SentThrough { get; set; } // email , sms, app.. 

        public DateTime SentAt { get; set; }

        public Trainer? Trainer { get; set; } = null;

        public Client? Client { get; set; } = null;

    }
}
