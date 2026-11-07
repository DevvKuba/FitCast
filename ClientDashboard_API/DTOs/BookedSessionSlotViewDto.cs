using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    public class BookedSessionSlotViewDto
    {
        public required int Id { get; set; }

        public required int ClientId { get; set; }

        public required string ClientName { get; set; }

        public int? BookingSeriesId { get; set; } // null for standalone slots

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required DateTime StartDateTime { get; set; } // UTC

        public required DateTime EndDateTime { get; set; } // UTC

        public required SessionStatus Status { get; set; }
    }
}
