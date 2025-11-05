namespace ClientDashboard_API.Entities
{
    public abstract class UserBase
    {
        public int Id { get; set; }

        public required string Email { get; set; }

        public required string FirstName { get; set; }

        public required string Surname { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PasswordHash { get; set; }
    }
}
