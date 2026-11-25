using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Services;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class TrainerAnalyticsServiceTests
    {
        private readonly TrainerFullMonthAnalyticsService _fullMonthAnalyticsService = new();
        private readonly TrainerCurrentMonthAnalyticsService _currentMonthAnalyticsService = new();

        [Fact]
        public void GetClientMetrics_CalculatesPercentagesWithDecimals()
        {
            // 20 clients at the start, 3 acquired and 1 churned over the month
            var records = Month(days: 30, activeClients: 20, sessions: 2, revenue: 100m, minutes: 60);
            records[5].ActiveClients = 23;
            for (int i = 6; i < records.Count; i++) records[i].ActiveClients = 23;
            records[20].ActiveClients = 22;
            for (int i = 21; i < records.Count; i++) records[i].ActiveClients = 22;

            var metrics = _fullMonthAnalyticsService.GetClientMetrics(records);

            Assert.Equal(3, metrics.AcquiredClients);
            Assert.Equal(15.0, metrics.AcquisitionPercentage);
            Assert.Equal(1, metrics.ChurnedClients);
            Assert.Equal(5.0, metrics.ChurnPercentage);
            Assert.Equal(10.0, metrics.NetGrowthPercentage);
        }

        [Fact]
        public void GetClientMetrics_ReturnsSessionsPerClientToOneDecimal()
        {
            // 70 sessions across an average of 20 clients
            var records = Month(days: 7, activeClients: 20, sessions: 10, revenue: 0m, minutes: 0);

            Assert.Equal(3.5, _fullMonthAnalyticsService.GetClientMetrics(records).AverageSessionsPerClient);
        }

        [Fact]
        public void GetRevenuePatterns_DividesByDaysWithSessions()
        {
            // 30 days recorded but sessions on only 10 of them, each earning 100
            var records = Month(days: 30, activeClients: 5, sessions: 0, revenue: 0m, minutes: 0);
            for (int i = 0; i < 10; i++)
            {
                records[i].SessionsToday = 2;
                records[i].RevenueToday = 100m;
            }

            var patterns = _fullMonthAnalyticsService.GetRevenuePatterns(records);

            Assert.Equal(10, patterns.MonthlyWorkingDays);
            Assert.Equal(100m, patterns.RevenuePerWorkingDay);
            Assert.Equal(Math.Round(1000m / (30 / 7m), 2), patterns.RevenuePerWorkingWeek);
        }

        [Fact]
        public void GetRevenuePatterns_HandlesAPartialMonthShorterThanAWeek()
        {
            var records = Month(days: 4, activeClients: 5, sessions: 1, revenue: 50m, minutes: 60);

            var patterns = _fullMonthAnalyticsService.GetRevenuePatterns(records);

            Assert.Equal(350m, patterns.RevenuePerWorkingWeek); // 200 over 4/7 of a week
        }

        [Fact]
        public void GetActivityPatterns_ReturnsMultipliersWhenTheAverageIsBelowOneSessionADay()
        {
            var records = Month(days: 14, activeClients: 5, sessions: 0, revenue: 0m, minutes: 0);
            records[0].SessionsToday = 3; // the 1st and 8th of November 2026 are both Sundays
            records[7].SessionsToday = 3;

            var patterns = _fullMonthAnalyticsService.GetActivityPatterns(records);

            Assert.NotEmpty(patterns.AllWeekdays);
            Assert.Contains(patterns.AllWeekdays, p => p.multiplier == 7.0);
        }

        [Fact]
        public void GetAllAnalyticMetrics_FlagsAPartialMonth()
        {
            var records = Month(days: 12, activeClients: 5, sessions: 1, revenue: 50m, minutes: 60);

            var analytics = _fullMonthAnalyticsService.GetAllAnalyticMetrics(records);

            Assert.True(analytics.IsPartialMonth);
            Assert.Equal(12, analytics.RecordedDays);
        }

        [Fact]
        public void GetAllAnalyticMetrics_DoesNotFlagAFullMonth()
        {
            var records = Month(days: 30, activeClients: 5, sessions: 1, revenue: 50m, minutes: 60);

            Assert.False(_fullMonthAnalyticsService.GetAllAnalyticMetrics(records).IsPartialMonth);
        }

        [Fact]
        public void GetCurrentMonthsAnalyticMetrics_DividesRevenueByDaysWithSessions()
        {
            var records = Month(days: 10, activeClients: 5, sessions: 0, revenue: 0m, minutes: 0);
            records[1].SessionsToday = 1;
            records[1].RevenueToday = 45m;
            records[4].SessionsToday = 2;
            records[4].RevenueToday = 90m;

            var analytics = _currentMonthAnalyticsService.GetCurrentMonthsAnalyticMetrics(records);

            Assert.Equal(67.5m, analytics.RevenuePerWorkingDay);
        }

        // Daily snapshots for November 2026, starting on the 1st
        private static List<TrainerDailyRevenue> Month(int days, int activeClients, int sessions, decimal revenue, int minutes)
        {
            var records = new List<TrainerDailyRevenue>();
            for (int day = 1; day <= days; day++)
            {
                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = 1,
                    AsOfDate = new DateOnly(2026, 11, day),
                    ActiveClients = activeClients,
                    SessionsToday = sessions,
                    RevenueToday = revenue,
                    TotalSessionDuration = minutes,
                    AverageSessionPrice = 50m
                });
            }
            return records;
        }
    }
}
