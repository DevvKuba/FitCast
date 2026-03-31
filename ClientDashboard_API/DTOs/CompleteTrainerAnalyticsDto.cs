using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class CompleteTrainerAnalyticsDto
    {
        // CLIENT METRICS
        public int BaseClients { get; set; }

        public int AcquiredClients { get; set; }

        public double AcquisitionPercentage { get; set; }

        public int ChurnedClients { get; set; }

        public double ChurnPercentage { get; set; }

        public int NetGrowth { get; set; }

        public double NetGrowthPercentage { get; set; }

        public int SessionsPerClient { get; set; }

        public int MonthlyClientSessions { get; set; }

        // REVENUE PATTERNS

        public decimal SessionsPrice { get; set; }

        public int MonthlyWorkingDays { get; set; }

        public double RevenuePerWorkingDay { get; set; }

        public double RevenuePerWorkingWeek { get; set; }

        public double RevenuePerWorkingMonth { get; set; }

        // ACTIVITY PATTERNS

        public required List<WeeklyMultiplier> BusiestDays { get; set; }

        public required List<WeeklyMultiplier> LightDays { get; set; }
    }
}
