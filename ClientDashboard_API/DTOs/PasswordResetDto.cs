namespace ClientDashboard_API.DTOs
{
    public class PasswordResetDto
    {
        public required int TokenId { get; set; }

        public required string NewPassword { get; set; }
    }
}
