using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Trunking.V1;

namespace ClientDashboard_API.Data
{
    public class TrainerDailyRevenueRepository(DataContext context) : ITrainerDailyRevenueRepository
    {
        public async Task AddTrainerDailyRevenueRecordAsync(TrainerDailyDataAddDto trainerInfo)
        {
            var trainerRevenueRecord = new TrainerDailyRevenue
            {
                TrainerId = trainerInfo.TrainerId,
                RevenueToday = trainerInfo.RevenueToday,
                MonthlyRevenueThusFar = trainerInfo.MonthlyRevenueThusFar,
                TotalSessionsThisMonth = trainerInfo.TotalSessionsThisMonth,
                NewClientsThisMonth = trainerInfo.NewClientsThisMonth,
                ActiveClients = trainerInfo.ActiveClients,
                AverageSessionPrice = trainerInfo.AverageSessionPrice,
                AsOfDate = trainerInfo.AsOfDate,
            };
            await context.TrainerDailyRevenue.AddAsync(trainerRevenueRecord);
        }

        public async Task<List<TrainerDailyRevenue>> GetAllRevenueRecordsForTrainerAsync(int trainerId)
        {
            var trainerRecords = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).ToListAsync();
            return trainerRecords;
        }

        public async Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId)
        {
            var latestRecord = await context.TrainerDailyRevenue.Where(t => t.TrainerId == trainerId).OrderByDescending(r => r.AsOfDate).FirstOrDefaultAsync();
            return latestRecord;
        }

        // maybe change to checking first day and then using that as the reference of sorts ?
        public async Task<List<TrainerDailyRevenue>> GetLastMonthsDayRecordsForTrainerAsync(int trainerId)
        {
            var lastMonthsDayRecords = await context.TrainerDailyRevenue
                .Where(r => r.TrainerId == trainerId &&
                r.AsOfDate.Day == DateTime.DaysInMonth(r.AsOfDate.Year, r.AsOfDate.Month))
                .OrderBy(r => r.AsOfDate)
                .ToListAsync();
            return lastMonthsDayRecords;
        }

        public async Task<List<TrainerDailyRevenue>> GetLastMonthsDayRecordsBasedOnFirstRecordAsync(int trainerId)
        {
            var firstRecord = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).OrderBy(r => r.AsOfDate).FirstOrDefaultAsync();

            var monthlyRecords = await context.TrainerDailyRevenue
                .Where(r => r.TrainerId == trainerId &&
                r.AsOfDate.Day == firstRecord!.AsOfDate.Day &&
                r.AsOfDate.Month != firstRecord.AsOfDate.Month)
                .OrderBy(r => r.AsOfDate)
                .ToListAsync();
            return monthlyRecords;
        }

        public async Task ResetTrainerDailyRevenueRecords(int trainerId)
        {
            var trainerRevenueRecords = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).ToListAsync();
            context.RemoveRange(trainerRevenueRecords);
        }

        public async Task AddTrainerDummyReveneRecordAsync(TrainerDailyRevenue trainerInfo)
        {
            await context.TrainerDailyRevenue.AddAsync(trainerInfo);
        }

        public async Task<bool> CanTrainerExtendRevenueRecordsAsync(int trainerId)
        {
            var records = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).ToListAsync();

            if (records.Count < 60) return false;
            return true;
        }
    }
}
