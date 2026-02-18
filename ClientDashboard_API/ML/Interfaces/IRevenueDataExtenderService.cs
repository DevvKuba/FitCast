using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Interfaces
{
    public interface IRevenueDataExtenderService
    {
        Task<List<TrainerRevenueData>> ProvideExtensionRecordsForRevenueDataAsync(int trainerId);

        Task FilterExtensionRevenueRecordsAsync(int trainerId);
    }
}
