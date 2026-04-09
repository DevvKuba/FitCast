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

        public int GetAllMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords)
        {
            var allMonthCount = revenueRecords
                .Select(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                .Distinct()
                .Count();
            return allMonthCount;
        }

        public int GetFullMonthCountsFromData(List<TrainerDailyRevenue> revenueRecords)
        {
            if(revenueRecords == null || revenueRecords.Count == 0)
            {
                return 0;
            }
            // only return counts of full months within passed in revenue records
            return revenueRecords
                 .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                 .Count(monthGroup =>
                 {
                     var daysInMonth = monthGroup
                     .Select(r => r.AsOfDate.Day)
                     .ToHashSet();

                     var lastDay = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);

                     return daysInMonth.Contains(1) && daysInMonth.Contains(lastDay);
                 });
        }

        public async Task<TrainerDailyRevenue?> GetLatestRevenueRecordForTrainerAsync(int trainerId)
        {
            var latestRecord = await context.TrainerDailyRevenue.Where(t => t.TrainerId == trainerId).OrderByDescending(r => r.AsOfDate).FirstOrDefaultAsync();
            return latestRecord;
        }

        public async Task<TrainerDailyRevenue?> GetFirstRevenueRecordForTrainerAsync(int trainerId)
        {
            var firstRecord = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).OrderBy(r => r.AsOfDate).FirstOrDefaultAsync();
            return firstRecord;
        }

        public async Task<List<TrainerDailyRevenue>> GetLastDayForEachMonthOfTrainerDataAsync(int trainerId)
        {
            var allRecords = await GetAllRevenueRecordsForTrainerAsync(trainerId);

            var lastMonthsDayRecords = allRecords
                .Where(r => r.TrainerId == trainerId &&
                r.AsOfDate.Day == DateTime.DaysInMonth(r.AsOfDate.Year, r.AsOfDate.Month))
                .OrderBy(r => r.AsOfDate)
                .ToList();
            return lastMonthsDayRecords;
        }

        public async Task<List<TrainerDailyRevenue>> GetLastMonthsRecordsAsync(int trainerId)
        {
            var latestRecord = await GetLatestRevenueRecordForTrainerAsync(trainerId);
            var previousMonth = latestRecord!.AsOfDate.Month - 1;

            var monthlyRecords = await context.TrainerDailyRevenue
                .Where(r => r.TrainerId == trainerId &&
                r.AsOfDate.Month == previousMonth)
                .OrderBy(r => r.AsOfDate)
                .ToListAsync();
            return monthlyRecords;
        }

        public async Task<List<TrainerDailyRevenue>> GetFirstFullMonthOfRevenueRecordsAsync(List<TrainerDailyRevenue> revenueRecords)
        {
            // get next month of records
            var firstRecord = await GetFirstRevenueRecordForTrainerAsync(revenueRecords.FirstOrDefault()!.TrainerId);

            var firstDayOfNewMonth = new DateOnly(firstRecord!.AsOfDate.Year, firstRecord.AsOfDate.Month, 1).AddMonths(1);

            var lastDayOfNewMonth = new DateOnly(firstDayOfNewMonth.Year, firstDayOfNewMonth.Month,
                DateTime.DaysInMonth(firstDayOfNewMonth.Year, firstDayOfNewMonth.Month));

            var firstNewMonthRecords = await context.TrainerDailyRevenue
                .Where(r => r.AsOfDate >= firstDayOfNewMonth && r.AsOfDate <= lastDayOfNewMonth)
                .ToListAsync();

            return firstNewMonthRecords;
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

        public bool DoRecordsIncludeFullMonths(List<TrainerDailyRevenue> revenueRecords, int monthsAccountedFor)
        {
            for (int i = 0; i < monthsAccountedFor; i++)
            {
                // extract months records each iteration - use instead of revenueRecords below

                var startOfMonth = revenueRecords.Where(r => r.AsOfDate.Day == 1);

                var endOfMonth = revenueRecords.Where(r => r.AsOfDate.Day == DateTime.DaysInMonth(r.AsOfDate.Year, r.AsOfDate.Month));

                // instead of first use i ?
                if (startOfMonth.Count() == 0 || endOfMonth.Count() == 0) return false;

                if (revenueRecords.Count != endOfMonth.First().AsOfDate.Day) return false;
            }
            return true;
        }

        public async Task ResetTrainerDailyRevenueRecordsAsync(int trainerId)
        {
            var trainerRevenueRecords = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).ToListAsync();
            context.RemoveRange(trainerRevenueRecords);
        }

        public async Task DeleteExtensionRecordsUpToDateAsync(TrainerDailyRevenue firstRealRecord)
        {
            var trainerRecords = await GetAllRevenueRecordsForTrainerAsync(firstRealRecord.TrainerId);

            var extendedRecords = trainerRecords.Where(r => r.AsOfDate < firstRealRecord.AsOfDate).ToList();

            foreach(TrainerDailyRevenue record in extendedRecords)
            {
                context.TrainerDailyRevenue.Remove(record);
            }
        }

        public async Task<TrainerDailyRevenue?> GetPreviousFullMonthLastRecordAsync(int trainerId)
        {
            var lastDayOfMonthlyRecords = await GetLastDayForEachMonthOfTrainerDataAsync(trainerId);

            return lastDayOfMonthlyRecords.Last();
        }
    }
}
