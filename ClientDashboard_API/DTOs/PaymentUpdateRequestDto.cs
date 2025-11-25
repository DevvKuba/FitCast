namespace ClientDashboard_API.DTOs
{
    public class PaymentUpdateRequestDto
    {
        public required int Id { get; set; }

        public required decimal Amount { get; set; }

        public required string Currency { get; set; }

        public required int NumberOfSessions { get; set; }

        public required string PaymentDate { get; set; }

        public required bool Confirmed { get; set; }
    }
}
