namespace ClientDashboard_API.Entities
{
    public class WorkoutData
    {
        public required string Title { get; set; }

        public required DateTime SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

        public int ExerciseCount { get; set; }
    }
}
