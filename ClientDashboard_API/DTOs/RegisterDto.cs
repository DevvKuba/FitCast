namespace ClientDashboard_API.DTOs
{
    public class RegisterDto
    {
        public required string Email { get; set; }

        public required string FirstName { get; set; }

        public required string Surname { get; set; }

        public required string Password { get; set; }
    }
}
