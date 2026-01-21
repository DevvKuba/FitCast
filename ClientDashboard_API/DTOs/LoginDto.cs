using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    public class LoginDto
    {
        public required string Email { get; set; }

        public required string Password { get; set; }

        public required UserRole Role { get; set; }
    }
}
