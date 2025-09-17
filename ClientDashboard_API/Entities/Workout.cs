namespace ClientDashboard_API.Entities
{
    public class Workout
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public required string ClientName { get; set; }
        public required string WorkoutTitle { get; set; }

        public DateOnly SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int? TotalBlockSessions { get; set; }

        public int ExerciseCount { get; set; }

        // navigration properties
        public Client Client { get; set; } = null!;

        // potentially add more data that can be stored 

    }
}
