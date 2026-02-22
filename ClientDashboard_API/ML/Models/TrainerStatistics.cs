namespace ClientDashboard_API.ML.Models
{
    public class TrainerStatistics
    {
        // manaully collected
        public int BaseActiveClients { get; set; }

        public decimal BaseSessionsPrice { get; set; }

        public int BaseSessionsPerMonth { get; set; }

        // computed 

        public int MyProperty { get; set; }

        public int MyProperty1 { get; set; }

        public int MonthlyWorkingDays { get; set; }
    }
}
