using ClientDashboard_API.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email must be provided")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password must be provided")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Role must be selected")]
        public required UserRole Role { get; set; }
    }
}
