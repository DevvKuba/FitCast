using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueRepository
    {
        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyDataAddDto trainerInfo);

        Task AddTrainerDummyReveneRecordAsync(TrainerDailyRevenue trainerInfo);

        Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetLastMonthsDayRecordsForTrainerAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetLastMonthsDayRecordsBasedOnFirstRecordAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId);

        Task<bool> CanTrainerExtendRevenueRecordsAsync(int trainerId);

        Task ResetTrainerDailyRevenueRecords(int trainerId);
    }
}
