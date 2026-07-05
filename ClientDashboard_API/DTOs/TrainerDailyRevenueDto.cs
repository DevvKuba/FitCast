namespace ClientDashboard_API.DTOs
{
    public class TrainerDailyRevenueDto
    {
        public int TrainerId { get; set; }

        public decimal RevenueToday { get; set; }

        public decimal MonthlyRevenueThusFar { get; set; }

        public int SessionsToday { get; set; }

        public int TotalSessionsThisMonth { get; set; }

        public int NewClientsThisMonth { get; set; }

        public int TotalSessionDuration { get; set; }

        public int ActiveClients { get; set; }

        public decimal AverageSessionPrice { get; set; }

        public DateOnly AsOfDate { get; set; }
    }
}
