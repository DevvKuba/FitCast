namespace ClientDashboard_API.DTOs
{
    public class MonthlyClientMetricsDto
    {
        public int BaseClients { get; set; }

        public int AcquiredClients { get; set; }

        public double AcquisitionPercentage { get; set; }

        public int ChurnedClients { get; set; }

        public double ChurnPercentage { get; set; }

        public int NetGrowth { get; set; }

        public double NetGrowthPercentage { get; set; }

        public int AverageSessionsPerClient { get; set; }

    }
}
