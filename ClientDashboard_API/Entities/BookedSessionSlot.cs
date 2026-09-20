using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Entities
{
    public class BookedSessionSlot
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public int ClientId { get; set; }

        public int? BookingSeriesId { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public SessionStatus Status { get; set; }

        public BookingSeries BookingSeries { get; set; } = null!;

        public Trainer Trainer { get; set; } = null!;

        public Client Client { get; set; } = null!;
    }
}
