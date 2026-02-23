using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.ML.Services
{
    public class RevenueDataExtenderService(IUnitOfWork unitOfWork) : IRevenueDataExtenderService
    {
        public async Task<TrainerDailyRevenue> ProvideExtensionRecordsForRevenueDataAsync(int trainerId)
        {
            var firstRevenueRecord = await unitOfWork.TrainerDailyRevenueRepository.GetFirstRevenueRecordForTrainerAsync(trainerId);
            var allRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

            var firstNewMonthsRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetFirstFullMonthOfRevenueRecordsAsync(allRevenueRecords);

            // a month from the first recorded trainer daily revenue record
            var monthlyRecords = await unitOfWork.TrainerDailyRevenueRepository.GetLastMonthsDayRecordsBasedOnFirstRecordAsync(firstRevenueRecord!);

            var monthlyWorkingDays = CalculateMonthlyWorkingDays(firstNewMonthsRevenueRecords);

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth
            var trainerStatistics = GenerateTrainerRevenueStatistics(monthlyRecords, monthlyWorkingDays);

            var weeklyMultipliers = CalculateWeekdayMultiplier(allRevenueRecords);

            // if there is at least 3 month of data
            if(allRevenueRecords.Count > 90)
            {
                // - calculate churn rate & acqusition rate - rather than looking at totalActiveClients at the end of the month
            }
            else
            {
                var historicalRevenuePatters = new MonthlyRevenuePatterns { acquisitionRate = 10, churnRate = 0.5 };
            }

            // -48 in order to ensure exact 48 months output
            var revenueRecords = DummyDataGenerator.GenerateExtendedRevenueData(trainerStatistics, null, weeklyMultipliers, trainerId, 48 - monthlyRecords.Count);

            return firstRevenueRecord!;

        }

        private TrainerStatistics GenerateTrainerRevenueStatistics(List<TrainerDailyRevenue> revenueRecords ,int workingDays)
        {
            var activeClients = Math.Round(revenueRecords.Average(r => r.ActiveClients), 0);

            var sessionPricing = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice), 0);

            var monthlySessions = Math.Round(revenueRecords.Average(r => r.TotalSessionsThisMonth), 0);

            var monthlyWorkingDays = workingDays;

            var statistics = new TrainerStatistics
            {
                BaseActiveClients = (int)activeClients,
                BaseSessionsPrice = sessionPricing,
                BaseSessionsPerMonth = (int)monthlySessions,
                MonthlyWorkingDays = monthlyWorkingDays
            };
            return statistics;
        }

        // all revenue records
        private MonthlyRevenuePatterns CalculateMonthlyClientChangeRates(List<TrainerDailyRevenue> revenueRecords)
        {
            var recordStartDay = revenueRecords.FirstOrDefault()!.AsOfDate.Day;
            var recordStartMonth = revenueRecords.FirstOrDefault()!.AsOfDate.Month;

            var startingMonthActiveClients = revenueRecords.FirstOrDefault()!.ActiveClients;
            var monthsAccountedFor = 0;
            
            double churnCount = 0;
            double acquisitionCount = 0;

            double churnRate = 0;
            double acquisitionRate = 0;


            for(int i = 0; i < revenueRecords.Count - 1; i++)
            {
                // indicates that we've iterating through a whole months records
                // can calculate the churn & acquisition rates and reset counts
                if (revenueRecords[i].AsOfDate.Day == recordStartDay && revenueRecords[i].AsOfDate.Month != recordStartMonth)
                {
                    acquisitionRate += (acquisitionCount / startingMonthActiveClients) * 100;
                    churnRate += (churnCount / startingMonthActiveClients) * 100;
                    monthsAccountedFor++;

                    acquisitionCount = 0;
                    churnCount = 0;
                }
                else
                {
                    if (revenueRecords[i + 1].ActiveClients > revenueRecords[i].ActiveClients)
                    {
                        acquisitionCount++;
                    }
                    else if (revenueRecords[i + 1].ActiveClients < revenueRecords[i].ActiveClients)
                    {
                        churnCount++;
                    }
                }
            }

            acquisitionRate = acquisitionRate / monthsAccountedFor;
            churnRate = churnRate / monthsAccountedFor;

            return new MonthlyRevenuePatterns { acquisitionRate = acquisitionRate , churnRate = churnRate };
        }

        // TODO complete logic
        //private double CalculateSessionMonthlyChurnRate()
        //{
        //    throw new NotImplementedException();
        //}

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

        private Dictionary<DayOfWeek, double> CalculateWeekdayMultiplier(List<TrainerDailyRevenue> allrevenueRecords)
        {
            double averageSessions = CalculateAverageDailySessions(allrevenueRecords);

            decimal averageSessionPrice = allrevenueRecords.FirstOrDefault()!.AverageSessionPrice;


            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weekdayAverages = allrevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Average(r => (double)(r.RevenueToday / averageSessionPrice))
                );

            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
            var multipliers = new Dictionary<DayOfWeek, double>();
            
            foreach(var day in weekdayAverages)
            {
                multipliers[day.Key] = day.Value / averageSessions;
            }

            return multipliers;
        }

        private double CalculateAverageDailySessions(List<TrainerDailyRevenue> revenueRecords)
        {
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.FirstOrDefault()!.Trainer.AverageSessionPrice;
            if (allSessions is null) return 0;

            return (double)allSessions / revenueRecords.Count;
        }

    }
}
