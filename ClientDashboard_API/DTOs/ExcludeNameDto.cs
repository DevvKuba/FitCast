using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class ExcludeNameDto
    {
        [Required(ErrorMessage = "Trainer ID must be provided")]
        public required int TrainerId { get; set; }

        [Required(ErrorMessage = "You must provide a name")]
        public required string Name { get; set; }

    }
}
