using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueRepository
    {
        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyDataAddDto trainerInfo);

        Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId);
    }
}
