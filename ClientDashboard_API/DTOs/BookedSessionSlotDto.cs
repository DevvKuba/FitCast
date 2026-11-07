using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class BookedSessionSlotDto : IValidatableObject
    {
        [Required(ErrorMessage = "Client ID must be provided")]
        public required int ClientId { get; set; }

        [Required(ErrorMessage = "You must provide a title")]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "You must provide a start time")]
        public required DateTime StartDateTime { get; set; } // UTC

        [Required(ErrorMessage = "You must provide an end time")]
        public required DateTime EndDateTime { get; set; } // UTC

        // runs automatically during model validation, returning a 400 before the controller action is reached
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // only a trailing Z binds as UTC, anything else would be stored against the wrong zone
            if (StartDateTime.Kind != DateTimeKind.Utc || EndDateTime.Kind != DateTimeKind.Utc)
            {
                yield return new ValidationResult("Session times must be provided in UTC", [nameof(StartDateTime), nameof(EndDateTime)]);
            }

            if (EndDateTime <= StartDateTime)
            {
                yield return new ValidationResult("Session end time must be after the start time", [nameof(EndDateTime)]);
            }
        }
    }
}
