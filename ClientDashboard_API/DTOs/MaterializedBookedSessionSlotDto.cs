using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;

namespace ClientDashboard_API.DTOs
{
    // Internal only - built by the materialization service from a BookingSeries, never bound from an HTTP request.
    public class MaterializedBookedSessionSlotDto : BookedSessionSlotDto
    {
        public required int TrainerId { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        // Navigation rather than BookingSeriesId - a newly created series has Id 0 until it's saved,
        // so EF links the slot through this reference and fills in the FK on Complete().
        public required BookingSeries BookingSeries { get; set; }
    }
}
