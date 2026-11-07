using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class BookingSeriesUpdateDto
    {
        [Required(ErrorMessage = "You must provide a title")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "You must provide a session duration")]
        public required int Duration { get; set; }

        [Required(ErrorMessage = "You must provide a start date")]
        public required DateOnly StartDate { get; set; } // trainer's local date

        [Required(ErrorMessage = "You must provide a start time")]
        public required TimeOnly StartTime { get; set; } // trainer's local wall-clock time
    }
}
