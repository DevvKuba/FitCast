using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueRepository
    {
        Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetLastDayForEachMonthOfTrainerDataAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetCurrentMonthsRevenueRecordsAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetSpecificFullMonthRecordsAsync(int trainerId, int month, int year);

        Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetFirstRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetRevenueRecordAtDateForTrainer(int trainerId, DateOnly date);

        int GetAllMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        int GetFullMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        List<FullMonthDto> GetFullMonthListFromData(List<TrainerDailyRevenue> revenueRecords);

        List<TrainerDailyRevenue> GetRecordsForFullMonths(List<TrainerDailyRevenue> revenueRecords);

        Task<bool> DoesTrainerDailyRevenueRecordExistForDateAsync(int trainerId, DateOnly date);

        Task UpdateTrainerRevenueRecordAtDateAsync(TrainerDailyRevenueDto trainerInfoDto);

        bool IsFullMonthPresent(List<TrainerDailyRevenue> revenueRecords);

        Task AddTrainerDailyRevenueDtoRecordAsync(TrainerDailyRevenueDto trainerInfo);

        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyRevenue trainerInfo);

        Task ResetTrainerDailyRevenueRecordsAsync(int trainerId);
    }
}
