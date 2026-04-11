using System.Text.Json.Serialization;

namespace ClientDashboard_API.Entities
{
    public class NotificationRecipientStatus
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int NotificationId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [JsonIgnore]
        public UserBase User { get; set; } = null!;

        [JsonIgnore]
        public Notification Notification { get; set; } = null!;

    }
}
