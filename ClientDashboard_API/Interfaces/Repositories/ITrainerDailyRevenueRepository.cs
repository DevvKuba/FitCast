using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface ITrainerDailyRevenueRepository
    {
        Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId);

        Task<List<AnalyticsMonthDto>> GetRecordedMonthsBeforeAsync(int trainerId, DateOnly beforeDate);

        Task<List<TrainerDailyRevenue>> GetSpecificFullMonthRecordsAsync(int trainerId, int month, int year);

        Task<List<TrainerDailyRevenue>> GetTrainerRecordsBetweenDatesAsync(int trainerId, DateOnly fromDate, DateOnly toDate);

        Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetRevenueRecordAtDateForTrainer(int trainerId, DateOnly date);

        int GetAllMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        int GetFullMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        List<FullMonthDto> GetFullMonthListFromData(List<TrainerDailyRevenue> revenueRecords);

        List<TrainerDailyRevenue> GetRecordsForFullMonths(List<TrainerDailyRevenue> revenueRecords);

        Task<bool> DoesTrainerDailyRevenueRecordExistForDateAsync(int trainerId, DateOnly date);

        Task UpdateTrainerRevenueRecordAtDateAsync(TrainerDailyRevenueDto trainerInfoDto);

        Task AddTrainerDailyRevenueDtoRecordAsync(TrainerDailyRevenueDto trainerInfo);

        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyRevenue trainerInfo);

        Task ResetTrainerDailyRevenueRecordsAsync(int trainerId);
    }
}
