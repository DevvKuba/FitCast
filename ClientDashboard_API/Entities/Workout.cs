namespace ClientDashboard_API.Entities
{
    public class Workout
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public required string ClientName { get; set; }
        public required string WorkoutTitle { get; set; }

        public DateOnly SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }

        public int Duration { get; set; }

        public DateTime? StartDateTime { get; set; } // UTC, null for workouts logged before session times existed

        public DateTime? EndDateTime { get; set; } // UTC

        // the calendar slot this workout belongs to; null for workouts logged before slots were linked
        public int? BookedSessionSlotId { get; set; }

        public Client? Client { get; set; } = null!;

        public BookedSessionSlot? BookedSessionSlot { get; set; } = null;

    }
}
