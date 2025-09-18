namespace ClientDashboard_API.Dto_s
{
    public class WorkoutSummaryDto
    {
        public required string Title { get; set; }

        public required DateOnly SessionDate { get; set; }

        public int ExerciseCount { get; set; }
    }
}
