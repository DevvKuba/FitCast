using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    public class NotificationResponseDto
    {
        public required int Id { get; set; }

        public int? TrainerId { get; set; }

        public int? ClientId { get; set; }

        public required string Message { get; set; }

        public required NotificationType ReminderType { get; set; }

        public required CommunicationType SentThrough { get; set; }

        public required NotificationAudience Audience { get; set; }

        public DateTime SentAt { get; set; }

        public required bool IsRead { get; set; }
    }
}
