namespace ClientDashboard_API.Dto_s
{
    public class WorkoutSessionDto
    {
        public required string Title { get; set; }

        public required DateTime SessionDate { get; set; }

        public int CurrentBlockSession { get; set; }

        public int TotalBlockSessions { get; set; }

        public int ExerciseCount { get; set; }
    }
}
