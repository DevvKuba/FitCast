using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class BookedSessionSlotDto
    {
        [Required(ErrorMessage = "Client ID must be provided")]
        public required int ClientId { get; set; }

        [Required(ErrorMessage = "You must provide a title")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "You must provide a start time")]
        public required DateTime StartDateTime { get; set; } // UTC

        [Required(ErrorMessage = "You must provide an end time")]
        public required DateTime EndDateTime { get; set; } // UTC
    }
}
