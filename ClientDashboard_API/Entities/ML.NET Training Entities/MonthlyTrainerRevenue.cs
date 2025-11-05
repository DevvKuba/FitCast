namespace ClientDashboard_API.Entities.ML.NET_Training_Entities
{
    public class MonthlyTrainerRevenue
    {
        public int Id { get; set; }

        public int? TrainerId { get; set; }

        public decimal MonthlyRevenue { get; set; }

        public int TotalSessions { get; set; }

        public int ActiveClients { get; set; }

        public int NewClients { get; set; }

        public int AverageSessionRate { get; set; }
    }
}
