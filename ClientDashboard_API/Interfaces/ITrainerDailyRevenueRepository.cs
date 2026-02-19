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

        Task<List<TrainerDailyRevenue>> GetLastMonthsDayRecordsBasedOnFirstRecordAsync(TrainerDailyRevenue firstRecord);

        Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetFirstRevenueRecordForTrainerAsync(int trainerId);

        Task<bool> CanTrainerExtendRevenueRecordsAsync(int trainerId);

        Task ResetTrainerDailyRevenueRecordsAsync(int trainerId);

        Task DeleteExtensionRecordsUpToDateAsync(TrainerDailyRevenue firstRealRecord);
    }
}
