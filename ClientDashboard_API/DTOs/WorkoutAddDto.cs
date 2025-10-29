

namespace ClientDashboard_API.DTOs
{
    public class WorkoutAddDto
    {
        public required string WorkoutTitle { get; set; }

        public required string ClientName { get; set; }

        public int ClientId { get; set; }

        public required string SessionDate { get; set; }

        public int ExerciseCount { get; set; }
    }
}
