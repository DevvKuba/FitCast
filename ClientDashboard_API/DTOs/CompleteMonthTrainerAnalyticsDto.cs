using ClientDashboard_API.Records;

namespace ClientDashboard_API.DTOs
{
    public class CompleteMonthTrainerAnalyticsDto
    {
        // CLIENT METRICS
        public int BaseClients { get; set; }

        public int AcquiredClients { get; set; }

        public double AcquisitionPercentage { get; set; }

        public int ChurnedClients { get; set; }

        public double ChurnPercentage { get; set; }

        public int NetGrowth { get; set; }

        public double NetGrowthPercentage { get; set; }

        public int AverageSessionsPerClient { get; set; }

        public int TotalClientSessions { get; set; }

        // REVENUE PATTERNS

        public decimal SessionsPrice { get; set; }

        public int MonthlyWorkingDays { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal RevenuePerWorkingDay { get; set; }

        public decimal RevenuePerWorkingWeek { get; set; }

        // SESSION DURATIONS

        public int TotalWorktimeMinutes { get; set; }

        public int AverageDailyWorktime { get; set; }

        public int AverageWeeklyWorktime { get; set; }


        // ACTIVITY PATTERNS

        public required List<WeeklyMultiplier> AllWeekdays { get; set; } 

        public required List<WeeklyMultiplier> BusiestDays { get; set; }

        public required List<WeeklyMultiplier> LightDays { get; set; }
    }
}
