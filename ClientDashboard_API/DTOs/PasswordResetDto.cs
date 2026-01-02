using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class PasswordResetDto
    {
        [Required(ErrorMessage = "Token ID is required")]
        public required int TokenId { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password needs to be at least 8 characters")]
        public required string NewPassword { get; set; }
    }
}
