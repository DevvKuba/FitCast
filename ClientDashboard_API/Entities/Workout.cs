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

        // set when the nightly session sync created this workout from a calendar slot; null for manually added workouts
        public int? BookedSessionSlotId { get; set; }

        // navigration properties
        public Client? Client { get; set; } = null!;

        public BookedSessionSlot? BookedSessionSlot { get; set; } = null;

        // potentially add more data that can be stored 

    }
}
