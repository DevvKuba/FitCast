using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class BookingSeriesStartDateUpdateDto
    {
        [Required(ErrorMessage = "You must provide a start date")]
        public required DateOnly StartDate { get; set; } // trainer's local date
    }
}
