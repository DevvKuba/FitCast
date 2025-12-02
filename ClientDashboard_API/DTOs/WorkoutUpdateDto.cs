namespace ClientDashboard_API.DTOs
{
    public class WorkoutUpdateDto
    {
        public required int Id { get; set; }
        public required string WorkoutTitle { get; set; }

        public required string SessionDate { get; set; }

        public required int ExerciseCount { get; set; }

        public required int Duration { get; set; }
    }
}
