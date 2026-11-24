namespace ClientDashboard_API.DTOs
{
    public class AnalyticsMonthDto
    {
        public required int Year { get; set; }

        public required int Month { get; set; }

        public required int RecordedDays { get; set; }

        public required bool IsPartial { get; set; } 
    }
}
