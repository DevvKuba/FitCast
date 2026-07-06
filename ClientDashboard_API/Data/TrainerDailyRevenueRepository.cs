using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio.Rest.Trunking.V1;
using Twilio.TwiML.Fax;

namespace ClientDashboard_API.Data
{
    public class TrainerDailyRevenueRepository(DataContext context, IMapper mapper) : ITrainerDailyRevenueRepository
    {
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
            return revenueRecords
                 .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                 .Count(monthGroup =>
                 {
                     var days = monthGroup
                     .Select(r => r.AsOfDate.Day)
                     .ToHashSet();

                     var lastDay = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);

                     return days.Count == lastDay && Enumerable.Range(1, days.Count).All(day => days.Contains(day));
                 });
        }

        public List<FullMonthDto> GetFullMonthListFromData(List<TrainerDailyRevenue> revenueRecords)
        {
            List<FullMonthDto> fullMonths = [];

             revenueRecords
                 .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                 .ForEach(monthGroup =>
                 {
                     var days = monthGroup
                     .Select(r => r.AsOfDate.Day)
                     .ToHashSet();

                     var lastDay = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);

                     if(days.Count == lastDay && Enumerable.Range(1, days.Count).All(day => days.Contains(day)))
                     {
                         fullMonths.Add( new FullMonthDto { Month = monthGroup.Key.Month, Year = monthGroup.Key.Year});
                     }
                 });
            return fullMonths;
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

        public async Task<List<TrainerDailyRevenue>> GetCurrentMonthsRevenueRecordsAsync(int trainerId)
        {
            var currentMonthsRecords = await context.TrainerDailyRevenue
                .Where( r => r.TrainerId == trainerId &&
                 r.AsOfDate.Month == DateTime.UtcNow.Month && 
                 r.AsOfDate.Year == DateTime.UtcNow.Year)
                .ToListAsync();

            return currentMonthsRecords;
        }

        public async Task<TrainerDailyRevenue?> GetRevenueRecordAtDateForTrainer(int trainerId, DateOnly date)
        {
            var revenueRecordAtDate = await context.TrainerDailyRevenue
                 .Where(r => r.TrainerId == trainerId &&
                 r.AsOfDate == date)
                 .FirstOrDefaultAsync();

            return revenueRecordAtDate;
        }

        public async Task<List<TrainerDailyRevenue>> GetSpecificFullMonthRecordsAsync(int trainerId, int month, int year)
        {
            var monthRecords = await context.TrainerDailyRevenue
                .Where(r => r.TrainerId == trainerId &&
                r.AsOfDate.Month == month &&
                r.AsOfDate.Year == year)
                .ToListAsync();

            return monthRecords;
        }

        public List<TrainerDailyRevenue> GetRecordsForFullMonths(List<TrainerDailyRevenue> revenueRecords)
        {
            return revenueRecords
                .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                .Where(monthGroup =>
                {
                    var days = monthGroup
                    .Select(r => r.AsOfDate.Day)
                    .ToHashSet();

                    var lastDay = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);

                    return days.Count == lastDay &&
                            Enumerable.Range(1, lastDay).All(day => days.Contains(day));
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .SelectMany(g => g.OrderBy(r => r.AsOfDate))
                .ToList();
        }

        public async Task<bool> DoesTrainerDailyRevenueRecordExistForDateAsync(int trainerId, DateOnly date)
        {
            var recordForDate = await GetRevenueRecordAtDateForTrainer(trainerId, date);

            if (recordForDate == null) return false;
            return true;
        }

        public async Task UpdateTrainerRevenueRecordAtDateAsync(TrainerDailyRevenueDto trainerInfoDto)
        {
            var revenueRecordAtDate = await GetRevenueRecordAtDateForTrainer(trainerInfoDto.TrainerId, trainerInfoDto.AsOfDate);

            mapper.Map(trainerInfoDto, revenueRecordAtDate);
        }

        public bool IsFullMonthPresent(List<TrainerDailyRevenue> revenueRecords)
        {
            if (revenueRecords.Count == 0 || revenueRecords == null) return false;

            var firstDayMonth = revenueRecords.First().AsOfDate.Month;

            var anyRecordsWithDifferentMonth = revenueRecords.Any(r => r.AsOfDate.Month != firstDayMonth);

            if (anyRecordsWithDifferentMonth) return false;
            return true;
        }

        public async Task AddTrainerDailyRevenueDtoRecordAsync(TrainerDailyRevenueDto trainerInfoDto)
        {
            var trainerInfo = mapper.Map<TrainerDailyRevenue>(trainerInfoDto);

            await context.TrainerDailyRevenue.AddAsync(trainerInfo);
        }

        public async Task AddTrainerDailyRevenueRecordAsync(TrainerDailyRevenue trainerInfo)
        {
            await context.TrainerDailyRevenue.AddAsync(trainerInfo);
        }

        public async Task ResetTrainerDailyRevenueRecordsAsync(int trainerId)
        {
            var trainerRevenueRecords = await context.TrainerDailyRevenue.Where(r => r.TrainerId == trainerId).ToListAsync();
            context.RemoveRange(trainerRevenueRecords);
        }

    }
}
