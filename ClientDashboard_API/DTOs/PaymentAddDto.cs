using System.ComponentModel.DataAnnotations;

namespace ClientDashboard_API.DTOs
{
    public class PaymentAddDto
    {
        public required int TrainerId { get; set; }

        public required int ClientId { get; set; }

        [Required(ErrorMessage = "Payment amount must be provided")]
        public required decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment sessions must be provided")]
        public required int NumberOfSessions { get; set; }

        [Required(ErrorMessage = "Payment date must be provided")]
        public required string PaymentDate { get; set; }

        [Required(ErrorMessage = "Payment status must be provided")]
        public required bool Confirmed { get; set; }
    }
}
