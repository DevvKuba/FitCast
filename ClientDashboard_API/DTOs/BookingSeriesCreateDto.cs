using ClientDashboard_API.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class BookingSeriesCreateDto
    {
        [Required(ErrorMessage = "Client ID must be provided")]
        public required int ClientId { get; set; }

        [Required(ErrorMessage = "You must provide a title")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "You must provide a session duration")]
        public required int Duration { get; set; }

        [Required(ErrorMessage = "You must provide a start date")]
        public required DateOnly StartDate { get; set; } // trainer's local date

        [Required(ErrorMessage = "You must provide a start time")]
        public required TimeOnly StartTime { get; set; } // trainer's local wall-clock time

        [Required(ErrorMessage = "You must provide a recurrence")]
        public required SessionRecurrence Recurrence { get; set; }
    }
}
