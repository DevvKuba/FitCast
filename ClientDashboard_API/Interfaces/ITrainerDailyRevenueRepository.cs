using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

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

        int GetAllMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        int GetFullMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        List<FullMonthDto> GetFullMonthListFromData(List<TrainerDailyRevenue> revenueRecords);

        public List<TrainerDailyRevenue> GetRecordsForFullMonths(List<TrainerDailyRevenue> revenueRecords);

        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyRevenue trainerInfo);

        Task ResetTrainerDailyRevenueRecordsAsync(int trainerId);
    }
}
