using ClientDashboard_API.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email address must be provided")]
        [EmailAddress(ErrorMessage = "Valid email is required")]
        public required string Email { get; set; }

        public required string FirstName { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Surname { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password needs to be at least 8 characters")]
        [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }

        public required UserRole Role { get; set; }

        public int? ClientId { get; set; }

        public int? ClientsTrainerId { get; set; }
    }
}
