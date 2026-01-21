

using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    public class UserDto
    {
        public string? FirstName { get; set; }

        public required int Id { get; set; }

        public required string Token { get; set; }

        public required UserRole Role { get; set; }

        // can expand upon , depending on what wants to be displayed in frontend
    }
}
