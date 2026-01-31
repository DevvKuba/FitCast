using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class WorkoutUpdateDto
    {
        public required int Id { get; set; }

        [Required(ErrorMessage = "Workout title must be provided")]
        public required string WorkoutTitle { get; set; }

        [Required(ErrorMessage = "Session date must be provided")]
        public required string SessionDate { get; set; }

        public required int ExerciseCount { get; set; }

        public required int Duration { get; set; }
    }
}
