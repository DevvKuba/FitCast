using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class ClientAddDto
    {
        [Required(ErrorMessage = "Must provide client name")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Must provide the clients monthly total block sessions")]
        public required int TotalBlockSessions { get; set; }

        public required string PhoneNumber { get; set; }

        public required int TrainerId { get; set; }
    }
}
