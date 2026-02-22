using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Services
{
    public class RevenueDataExtenderService(IUnitOfWork unitOfWork) : IRevenueDataExtenderService
    {
        public async Task<TrainerDailyRevenue> ProvideExtensionRecordsForRevenueDataAsync(int trainerId)
        {
            var firstRevenueRecord = await unitOfWork.TrainerDailyRevenueRepository.GetFirstRevenueRecordForTrainerAsync(trainerId);
            var allRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

            var firstNewMonthsRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetFirstFullMonthOfRevenueRecordsAsync(allRevenueRecords);

            var monthlyWorkingDays = CalculateMonthlyWorkingDays(firstNewMonthsRevenueRecords);

            // a month from the first recorded trainer daily revenue record
            var monthlyRecords = await unitOfWork.TrainerDailyRevenueRepository.GetLastMonthsDayRecordsBasedOnFirstRecordAsync(firstRevenueRecord!);

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth
            var trainerStatistics = GenerateTrainerRevenueStatistics(monthlyRecords, monthlyWorkingDays);

            var revenueRecords = DummyDataGenerator.GenerateExtendedRevenueData(trainerStatistics, trainerId, 48 - monthlyRecords.Count);

            return firstRevenueRecord!;

        }

        private TrainerStatistics GenerateTrainerRevenueStatistics(List<TrainerDailyRevenue> revenueRecords, int workingDays)
        {
            var activeClients = Math.Round(revenueRecords.Average(r => r.ActiveClients), 0);

            var sessionPricing = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice), 0);

            var monthlySessions = Math.Round(revenueRecords.Average(r => r.TotalSessionsThisMonth), 0);

            var monthlyGrowth = CalculateSessionMonthlyGrowth(revenueRecords.Select(r => r.TotalSessionsThisMonth).ToList());

            var monthlyWorkingDays = workingDays;

            var statistics = new TrainerStatistics
            {
                BaseActiveClients = (int)activeClients,
                BaseSessionsPrice = sessionPricing,
                BaseSessionsPerMonth = (int)monthlySessions,
                SessionMonthlyGrowth = monthlyGrowth,
                MonthlyWorkingDays = monthlyWorkingDays
            };
            return statistics;
        }

        private double CalculateSessionMonthlyGrowth(List<int> monthlySessions)
        {
            var totalPercentageChanges = 0;

            for(int i = 0; i < monthlySessions.Count - 1; i++)
            {
                totalPercentageChanges += (monthlySessions[i + 1] - monthlySessions[i]) / monthlySessions[i] * 100;
            }
            // if no variance is present, add a slight increase
            if (totalPercentageChanges == 0) return 0.05;

            return totalPercentageChanges / (monthlySessions.Count - 1);
        }

        private int CalculateMonthlyWorkingDays(List<TrainerDailyRevenue> fullMonthRecords)
        {
            int workingDays = 0;

            foreach(var record in fullMonthRecords)
            {
                if(record.RevenueToday > 0)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        private int CalculateWeekdayMultiplier(List<TrainerDailyRevenue> allrevenueRecords)
        {
            // weekday :  (number of occurances : totalSessionsValue)
            Dictionary<int, (double, double)> weekdayDict = new Dictionary<int, (double, double)>
            {
                { 0, (0,0) }, // sun
                { 1, (0,0) }, // mon
                { 2, (0,0) }, // tues
                { 3, (0,0) }, // wed
                { 4, (0,0) }, // thu
                { 5, (0,0) }, // fri
                { 6, (0,0) }, // sat

            };

            double averageSessions = CalculateAverageDailySessions(allrevenueRecords);
            decimal averageTrainerSessionPrice = allrevenueRecords.FirstOrDefault()!.AverageSessionPrice;


            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            foreach (var record in allrevenueRecords)
            {
                var dayOfTheWeek = (int)record.AsOfDate.DayOfWeek;
                double occurances;
                double totalSessions;

                switch (dayOfTheWeek)
                {
                    case 0:
                        occurances = weekdayDict[0].Item2;
                        totalSessions = weekdayDict[0].Item2;

                        weekdayDict[0] = (occurances++, totalSessions += (double)(record.RevenueToday / averageTrainerSessionPrice));
                        break;
                }

            }
            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) ?
        }

        public double CalculateAverageDailySessions(List<TrainerDailyRevenue> revenueRecords)
        {
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.FirstOrDefault()!.Trainer.AverageSessionPrice;
            if (allSessions is null) return 0;

            return (double)allSessions / revenueRecords.Count;
        }

    }
}
