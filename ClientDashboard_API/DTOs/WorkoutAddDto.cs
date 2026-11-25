

using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class WorkoutAddDto : IValidatableObject
    {
        [Required(ErrorMessage = "Must provide a workout title")]
        public required string WorkoutTitle { get; set; }

        [Required(ErrorMessage = "Must provide a client name title")]
        public required string ClientName { get; set; }

        [Required(ErrorMessage = "Must select a client already in Client Info page")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Must provide a session date")]
        public required string SessionDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A session duration must be provided")]
        public int Duration { get; set; }

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
