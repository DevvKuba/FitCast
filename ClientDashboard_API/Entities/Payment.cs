namespace ClientDashboard_API.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public int? ClientId { get; set; }

        public decimal Amount { get; set; }

        public required string Currency { get; set; }

        public required int NumberOfSessions { get; set; }

        public required DateOnly PaymentDate { get; set; }


        public Trainer Trainer { get; set; } = null!;

        public Client? Client { get; set; } = null;


    }
}
