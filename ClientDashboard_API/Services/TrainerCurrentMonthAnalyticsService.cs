using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerCurrentMonthAnalyticsService : ITrainerCurrentMonthAnalyticsService
    {
        public CurrentMonthTrainerAnalyticsDto GetCurrentMonthsAnalyticMetrics(List<TrainerDailyRevenue> currentRevenueRecords)
        {
            var baseClients = (int)currentRevenueRecords.Average(r => r.ActiveClients);

            var totalClientSessions = currentRevenueRecords.Sum(r => r.SessionsToday);

            var totalRevenue = currentRevenueRecords.Sum(r => r.RevenueToday);

            var totalWorktimeMinutes = currentRevenueRecords.Sum(r => r.TotalSessionDuration);

            var averageDailyRevenue = Math.Round(totalRevenue / currentRevenueRecords.Count);

            var weeklySessionCounts = WeekdayActivityPatternHelper.GetNumberOfSessionsForWeekdays(currentRevenueRecords);

            return new CurrentMonthTrainerAnalyticsDto
            {
                BaseClients = baseClients,
                MonthlyClientSessions = totalClientSessions,
                TotalRevenue = totalRevenue,
                TotalWorktimeMinutes = totalWorktimeMinutes,
                RevenuePerWorkingDay = averageDailyRevenue,
                WeeklySessionsCounts = weeklySessionCounts
            };
        }
    }
}
