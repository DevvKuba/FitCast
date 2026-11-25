using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class WorkoutUpdateDto : IValidatableObject
    {
        public required int Id { get; set; }

        [Required(ErrorMessage = "Workout title must be provided")]
        public required string WorkoutTitle { get; set; }

        [Required(ErrorMessage = "Session date must be provided")]
        public required string SessionDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A session duration must be provided")]
        public required int Duration { get; set; }

        public required DateTime StartDateTime { get; set; } // UTC; the session date is taken from it in the trainer's zone

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDateTime.Kind != DateTimeKind.Utc)
            {
                yield return new ValidationResult("Session start time must be provided in UTC", [nameof(StartDateTime)]);
            }
        }
    }
}
