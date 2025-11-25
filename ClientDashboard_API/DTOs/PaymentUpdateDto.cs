namespace ClientDashboard_API.DTOs
{
    public class PaymentUpdateDto
    {
        public required int Id { get; set; }

        public decimal? Amount { get; set; }

        public string? Currency { get; set; }

        public int? NumberOfSessions { get; set; }

        public DateOnly? PaymentDate { get; set; }

        public bool? Confirmed { get; set; }
    }
}
