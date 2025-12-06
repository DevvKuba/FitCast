namespace ClientDashboard_API.Entities.ML.NET_Training_Entities
{
    public class TrainerDailyRevenue
    {
        public int Id { get; set; }

        public int TrainerId { get; set; }

        public decimal RevenueToday { get; set; }

        public decimal MonthlyRevenueThusFar { get; set; }

        public int TotalSessionsThisMonth { get; set; }

        public int NewClientsThisMonth { get; set; }

        public int ActiveClients { get; set; }

        public decimal AverageSessionPrice { get; set; }

        public DateOnly AsOfDate { get; set; }

        public Trainer Trainer { get; set; } = null!;
    }
}
