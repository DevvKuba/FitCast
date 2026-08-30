using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Dto_s
{
    public class ClientUpdateDto
    {
        [Required(ErrorMessage = "Must supply Id field")]
        public required int Id { get; set; }

        [Required(ErrorMessage = "Must fill in Client Name field")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Must select activity status")]
        public required bool IsActive { get; set; }

        [Required(ErrorMessage = "Must fill in Current Session field")]
        public required int CurrentBlockSession { get; set; }

        [Required(ErrorMessage = "Must fill in Block Session field")]
        public required int TotalBlockSessions { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
