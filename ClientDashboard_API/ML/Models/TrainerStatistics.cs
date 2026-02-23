namespace ClientDashboard_API.ML.Models
{
    public class TrainerStatistics
    {
        // manaully collected
        public int BaseActiveClients { get; set; }

        public decimal BaseSessionsPrice { get; set; }

        // computed 

        public int AverageClientMonthlySessions { get; set; }

        public int MonthlyWorkingDays { get; set; }
    }
}
