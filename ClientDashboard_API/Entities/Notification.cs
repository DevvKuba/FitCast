namespace ClientDashboard_API.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public int? ClientId { get; set; }

        public required string Message { get; set; }

        public required string ReminderType { get; set; }

        public required string SentThrough { get; set; } // email , sms.. 

        public DateTime SentAt { get; set; }
    }
}
