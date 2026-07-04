namespace ClientDashboard_API.DTOs
{
    public class CurrentMonthTrainerAnalyticsDto
    {
        public int BaseClients { get; set; }

        public int MonthlyClientSessions { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalWorktimeMinutes { get; set; }

        public decimal RevenuePerWorkingDay { get; set; }

        // activity patterns that just - weekday sessions
        // display the number of sessions on each day so far
    }
}
