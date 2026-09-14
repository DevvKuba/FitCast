using ClientDashboard_API.Enums;

namespace ClientDashboard_API.Entities
{
    public class BookingSeries
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public int ClientId { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public DateOnly StartDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public required int Duration { get; set; }

        public SessionRecurrence Recurrence { get; set; }

        public Trainer Trainer { get; set; } = null!;

        public Client Client { get; set; } = null!;
    }
}
