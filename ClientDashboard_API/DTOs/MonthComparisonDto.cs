namespace ClientDashboard_API.DTOs
{
    public class MonthComparisonDto
    {
        public int BaseClientsChange { get; set; }

        public int SessionsChange { get; set; }

        public decimal EarnedRevenueChange { get; set; }

        public int WorktimeMinutesChange { get; set; }

        public int AcquiredClientsChange { get; set; }

        public int ChurnedClientsChange { get; set; }

        public bool ComparedMonthIsPartial { get; set; } 
    }
}
