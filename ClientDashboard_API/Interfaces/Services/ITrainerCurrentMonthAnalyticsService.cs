using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface ITrainerCurrentMonthAnalyticsService
    {
        CurrentMonthTrainerAnalyticsDto GetCurrentMonthsAnalyticMetrics(List<TrainerDailyRevenue> currentRevenueRecords);
    }
}
