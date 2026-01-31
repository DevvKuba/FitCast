using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.Dto_s
{
    public class ClientUpdateDto
    {
        [Required(ErrorMessage = "Must fill in Client Name field")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Must select activity status")]
        public bool? IsActive { get; set; }

        [Required(ErrorMessage = "Must fill in Current Session field")]
        public int? CurrentBlockSession { get; set; }

        [Required(ErrorMessage = "Must fill in Block Session field")]
        public int? TotalBlockSessions { get; set; }
    }
}
