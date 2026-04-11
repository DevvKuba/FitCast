namespace ClientDashboard_API.DTOs
{
    public class NotificationReadStatusDto
    {
        public required int UserId { get; set; }

        public required List<int> NotificationIds { get; set; }

    }
}
