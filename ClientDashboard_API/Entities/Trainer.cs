using ClientDashboard_API.Helpers;

namespace ClientDashboard_API.Entities
{
    public class Trainer : UserBase
    {
        public string? BusinessName { get; set; }

        public string TimeZoneId { get; set; } = SupportedTimeZones.Default;

        public decimal? AverageSessionPrice { get; set; }

        public bool EmailVerified { get; set; } = false;

        // when on, the nightly session sync turns finished calendar sessions into workouts and counts them
        public bool CalendarSyncEnabled { get; set; } = true;

        public bool AutoPaymentSetting { get; set; } = false;

        public string? DefaultCurrency { get; set; }

        public List<Client> Clients { get; set; } = [];
    }
}
