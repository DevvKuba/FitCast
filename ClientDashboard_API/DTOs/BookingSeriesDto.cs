using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    public class BookingSeriesDto
    {
        public required int Id { get; set; }

        public required int ClientId { get; set; }

        public required string ClientName { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required int Duration { get; set; } // minutes

        public required DateOnly StartDate { get; set; } // trainer's local date

        public required TimeOnly StartTime { get; set; } // trainer's local wall-clock time

        public required SessionRecurrence Recurrence { get; set; }
    }
}
