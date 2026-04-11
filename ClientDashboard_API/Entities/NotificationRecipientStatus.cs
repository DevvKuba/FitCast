namespace ClientDashboard_API.Entities
{
    public class NotificationRecipientStatus
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int NotificationId { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }

        public required UserBase User { get; set; }

        public required Notification Notification { get; set; } 

    }
}
