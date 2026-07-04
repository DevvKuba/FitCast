using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class CurrentMonthTrainerAnalyticsDto
    {
        public int BaseClients { get; set; }

        public int MonthlyClientSessions { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalWorktimeMinutes { get; set; }

        public decimal RevenuePerWorkingDay { get; set; }

        public List<WeeklySessionsCount> WeeklySessionsCounts { get; set; } = [];
    }
}
