namespace ClientDashboard_API.DTOs
{
    public class WorkoutUpdateDto
    {
        public string? Title { get; set; }

        public DateOnly? SessionDate { get; set; }

        public int? ExerciseCount { get; set; }
    }
}
