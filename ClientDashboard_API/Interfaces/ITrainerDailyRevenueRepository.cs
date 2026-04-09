using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueRepository
    {
        Task AddTrainerDailyRevenueRecordAsync(TrainerDailyDataAddDto trainerInfo);

        Task AddTrainerDummyReveneRecordAsync(TrainerDailyRevenue trainerInfo);

        Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetLastDayForEachMonthOfTrainerDataAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetLastMonthsRecordsAsync(int trainerId);

        Task<List<TrainerDailyRevenue>> GetFirstFullMonthOfRevenueRecordsAsync(List<TrainerDailyRevenue> revenueRecords);

        Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetFirstRevenueRecordForTrainerAsync(int trainerId);

        Task<TrainerDailyRevenue?> GetPreviousFullMonthLastRecordAsync(int trainerId);

        int GetAllMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        int GetFullMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords);

        Task<bool> CanTrainerExtendRevenueRecordsAsync(int trainerId);

        bool DoRecordsIncludeFullMonths(List<TrainerDailyRevenue> revenueRecords, int monthsAccountedFor);

        Task ResetTrainerDailyRevenueRecordsAsync(int trainerId);

        Task DeleteExtensionRecordsUpToDateAsync(TrainerDailyRevenue firstRealRecord);
    }
}
