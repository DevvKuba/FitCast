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

        public DateTime? StartDateTime { get; set; } // UTC; when missing an existing start keeps its clock time on the new date

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDateTime.HasValue && StartDateTime.Value.Kind != DateTimeKind.Utc)
            {
                yield return new ValidationResult("Session start time must be provided in UTC", [nameof(StartDateTime)]);
            }
        }
    }
}
