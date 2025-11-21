namespace ClientDashboard_API.DTOs
{
    public class PaymentAddDto
    {
        public required int TrainerId { get; set; }

        public required int ClientId { get; set; }

        public required decimal Amount { get; set; }

        public required string Currency { get; set; }

        public required int NumberOfSessions { get; set; }

        public required DateOnly PaymentDate { get; set; }
    }
}
