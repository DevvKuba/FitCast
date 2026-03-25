using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerAnalyticsService
    {
        ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords);

        RevenuePatternsDto GetRevenuePatterns(List<TrainerDailyRevenue> revenueRecords);

        ActivityPatternsDto GetActivityPatterns(List<TrainerDailyRevenue> revenueRecords);

    }
}
