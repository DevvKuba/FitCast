namespace ClientDashboard_API.Entities
{
    public class Trainer : UserBase
    {
        public string? BusinessName { get; set; }

        public decimal? AverageSessionPrice { get; set; }

        public string? WorkoutRetrievalApiKey { get; set; }

        public bool EmailVerified { get; set; } = false;

        public bool AutoWorkoutRetrieval { get; set; } = false;

        public bool AutoPaymentSetting { get; set; } = false;

        public string? DefaultCurrency { get; set; }

        public List<string> ExcludedNames { get; set; } = [];

        public List<Client> Clients { get; set; } = [];
    }
}
