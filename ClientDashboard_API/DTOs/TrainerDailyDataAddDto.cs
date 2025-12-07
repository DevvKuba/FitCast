using ClientDashboard_API.Data.Migrations;

namespace ClientDashboard_API.DTOs
{
    public class TrainerDailyDataAddDto
    {
        public int TrainerId { get; set; }

        public decimal RevenueToday { get; set; }

        public decimal MonthlyRevenueThusFar { get; set; }

        public int TotalSessionsThisMonth { get; set; }

        public int NewClientsThisMonth { get; set; }

        public int ActiveClients { get; set; }

        public decimal AverageSessionPrice { get; set; }

        public DateOnly AsOfDate { get; set; }
    }
}
