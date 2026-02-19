using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Interfaces
{
    public interface IRevenueDataExtenderService
    {
        Task<TrainerDailyRevenue> ProvideExtensionRecordsForRevenueDataAsync(int trainerId);
    }
}
