namespace ClientDashboard_API.Entities
{
    public class Trainer
    {
        public string? BusinessName { get; set; }

        public decimal? AverageSessionPrice { get; set; }

        public string? WorkoutRetrievalApiKey { get; set; }

        public string? DefaultCurrency { get; set; }

        public List<Client> Clients { get; set; } = [];
    }
}
