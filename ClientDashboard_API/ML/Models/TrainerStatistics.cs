namespace ClientDashboard_API.ML.Models
{
    public class TrainerStatistics
    {
        // manaully collected
        public int BaseActiveClients { get; set; }

        public decimal BaseSessionsPrice { get; set; }

        public int BaseSessionsPerMonth { get; set; }

        // computed 
        public double SessionMonthlyGrowth { get; set; }
    }
}
