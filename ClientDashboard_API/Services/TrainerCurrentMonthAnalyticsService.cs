using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerCurrentMonthAnalyticsService : ITrainerCurrentMonthAnalyticsService
    {
        public CurrentMonthTrainerAnalyticsDto GetCurrentMonthsAnalyticMetrics(List<TrainerDailyRevenue> currentRevenueRecords)
        {
            var baseClients = (int)Math.Round(currentRevenueRecords.Average(r => r.ActiveClients));

            var totalRevenue = currentRevenueRecords.Sum(r => r.RevenueToday);

            var averageDailyRevenue = Math.Round(totalRevenue / currentRevenueRecords.Count);

            var totalClientSessions = GetTotalClientSessions(currentRevenueRecords);

            return new CurrentMonthTrainerAnalyticsDto
            {
                BaseClients = baseClients,
                MonthlyClientSessions = totalClientSessions,
                TotalRevenue = totalRevenue,
                RevenuePerWorkingDay = (decimal)totalRevenue.RevenuePerWorkingDay,
            };
        }

        // TODO may be faulty if the trainer changes their session pricing consistently.. fix later
        private int GetTotalClientSessions(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var totalClientSessions = allRevenueRecords.Sum(r => r.RevenueToday / r.AverageSessionPrice);

            return (int)Math.Round(totalClientSessions);
        }
    }
}
