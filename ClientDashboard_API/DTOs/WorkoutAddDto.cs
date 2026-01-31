

using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class WorkoutAddDto
    {
        [Required(ErrorMessage = "Must provide a workout title")]
        public required string WorkoutTitle { get; set; }

        [Required(ErrorMessage = "Must provide a client name title")]
        public required string ClientName { get; set; }

        [Required(ErrorMessage = "Must select a client already in Client Info page")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Must provide a session date")]
        public required string SessionDate { get; set; }

        public int ExerciseCount { get; set; }

        public int Duration { get; set; }
    }
}
