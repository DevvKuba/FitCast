using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class PasswordResetDto
    {
        [Required(ErrorMessage = "Raw token is required")]
        public required string RawToken { get; set; }

        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password needs to be at least 8 characters")]
        [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        public required string NewPassword { get; set; }
    }
}
