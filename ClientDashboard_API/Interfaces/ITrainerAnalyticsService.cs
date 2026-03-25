using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerAnalyticsService
    {

        TrainerStatistics GetTrainerStatistics(List<TrainerDailyRevenue> allRevenueRecords, int workingDays, int averageMonthlySessionsPerClient);

        MonthlyRevenuePatterns CalculateMonthlyClientChangeRates(List<TrainerDailyRevenue> allRevenueRecords);

        int GetWorkingDayMetrics(List<TrainerDailyRevenue> allRevenueRecords);

        Dictionary<DayOfWeek, double> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allrevenueRecords);

        double CalculateAverageDailySessions(List<TrainerDailyRevenue> revenueRecords);

        int GetEngagementMetrics(List<TrainerDailyRevenue> allRevenueRecords);

    }
}
