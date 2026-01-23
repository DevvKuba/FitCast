using ClientDashboard_API.Entities;

namespace ClientDashboard_API.DTOs
{
    public class NotificationReadStatusDto
    {
        public required List<Notification> ReadNotificationsList { get; set; }

    }
}
