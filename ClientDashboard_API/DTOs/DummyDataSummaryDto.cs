namespace ClientDashboard_API.DTOs
{
    public class DummyDataSummaryDto
    {
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = string.Empty;
        public int RecordsGenerated { get; set; }
        public string DateRange { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
        public int StartingActiveClients { get; set; }
        public int EndingActiveClients { get; set; }
        public string Scenario { get; set; } = string.Empty;
    }
}
