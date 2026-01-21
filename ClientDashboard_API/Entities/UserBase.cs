using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Entities
{
    public abstract class UserBase
    {
        public int Id { get; set; }

        public required UserRole Role { get; set; }

        public required string FirstName { get; set; }

        public string? Email { get; set; }

        public string? Surname { get; set; }

        public string? PhotoUrl { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PasswordHash { get; set; }

        public bool NotificationsEnabled { get; set; } = false;

        public DateTime CreatedAt { get; private set; }
    }
}
