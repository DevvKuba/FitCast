namespace ClientDashboard_API.DTOs
{
    public class RegisterDto
    {
        public required string Email { get; set; }

        public required string FirstName { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Surname { get; set; }

        public required string Password { get; set; }

        public required string Role { get; set; }

        public int? ClientId { get; set; }

        public int? ClientsTrainerId { get; set; }
    }
}
