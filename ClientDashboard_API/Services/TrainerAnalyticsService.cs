using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Models;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.Services
{
    public class TrainerAnalyticsService(IUnitOfWork unitOfWork) : ITrainerAnalyticsService
    {
        public ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords)
        {

        }

        public RevenuePatternsDto GetRevenuePatterns(List<TrainerDailyRevenue> revenueRecords)
        {

        }

        public ActivityPatternsDto GetActivityPatterns(List<TrainerDailyRevenue> revenueRecords)
        {

        }
        private TrainerStatistics GetTrainerStatistics(List<TrainerDailyRevenue> allRevenueRecords ,int workingDays, int averageMonthlySessionsPerClient)
        {
            // better to only account for recent month, is it because it's closer to the next month to come
            // which is predicted for
            var latestMonth = allRevenueRecords.OrderByDescending(r => r.AsOfDate).Take(30).ToList();
            var activeClients = Math.Round(latestMonth.Average(r => r.ActiveClients), 0);

            var sessionPricing = Math.Round(allRevenueRecords.Average(r => r.AverageSessionPrice), 0);

            var monthlyWorkingDays = workingDays;

            var statistics = new TrainerStatistics
            {
                BaseActiveClients = (int)activeClients,
                BaseSessionsPrice = sessionPricing,
                AverageClientMonthlySessions = averageMonthlySessionsPerClient,
                MonthlyWorkingDays = monthlyWorkingDays
            };
            return statistics;
        }

        private MonthlyRevenuePatterns CalculateMonthlyClientChangeRates(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var recordStartDay = allRevenueRecords.First().AsOfDate.Day;
            var recordStartMonth = allRevenueRecords.First().AsOfDate.Month;

            var startingMonthActiveClients = allRevenueRecords.First().ActiveClients;
            var monthlyPairsAccountedFor = 0;
            
            double churnCount = 0;
            double acquisitionCount = 0;

            double churnRate = 0;
            double acquisitionRate = 0;

            int totalMonthsOfData = allRevenueRecords
                .Select(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                .Distinct()
                .Count();

            for(int i = 0; i < allRevenueRecords.Count - 1; i++)
            {
                // indicates that we've iterating through a whole months records
                // can calculate the churn & acquisition rates and reset counts
                if (allRevenueRecords[i].AsOfDate.Day == recordStartDay && allRevenueRecords[i].AsOfDate.Month != recordStartMonth)
                {
                    acquisitionRate += (acquisitionCount / startingMonthActiveClients) * 100;
                    churnRate += (churnCount / startingMonthActiveClients) * 100;
                    monthlyPairsAccountedFor++;

                    acquisitionCount = 0;
                    churnCount = 0;
                }
                else
                {
                    // compares if the next record's active clients have increased / decreased comapred to the current records
                    if (allRevenueRecords[i + 1].ActiveClients > allRevenueRecords[i].ActiveClients)
                    {
                        acquisitionCount += allRevenueRecords[i + 1].ActiveClients - allRevenueRecords[i].ActiveClients;
                    }
                    else if (allRevenueRecords[i + 1].ActiveClients < allRevenueRecords[i].ActiveClients)
                    {
                        churnCount += allRevenueRecords[i].ActiveClients - allRevenueRecords[i + 1].ActiveClients;
                    }
                }
            }

            acquisitionRate = acquisitionRate / monthlyPairsAccountedFor;
            churnRate = churnRate / monthlyPairsAccountedFor;

            // dampening at less than 6 months of data
            if(totalMonthsOfData < 6)
            {
                // reduce rates by 50% to prevent startup phase explosion
                acquisitionRate *= 0.5;
                churnRate *= 0.5;

                // further cap at 15% acquisition & 12% churn 
                acquisitionRate = Math.Min(acquisitionRate, 15.0);
                churnRate = Math.Min(churnRate, 12.0);
            }

            return new MonthlyRevenuePatterns { acquisitionRate = acquisitionRate , churnRate = churnRate };
        }

        private int GetWorkingDayMetrics(List<TrainerDailyRevenue> allRevenueRecords)
        {
            // get working days from first record day to next month with that same record day
            var monthlyWorkingDays = 0;

            var monthlyPairsAccountedFor = 0;
            var nonWorkingDays = 0;

            var firstRecord = allRevenueRecords.First();

            foreach(var record in allRevenueRecords)
            {
                if (record.RevenueToday == 0)
                {
                    nonWorkingDays++;
                }
                if(record.AsOfDate.Day == firstRecord.AsOfDate.Day && record.AsOfDate.Month != firstRecord.AsOfDate.Month)
                {
                    var previousMonth = record.AsOfDate.AddMonths(-1);
                    var daysInbetween = (int)(DateTime.Parse(record.AsOfDate.ToString()) - DateTime.Parse(previousMonth.ToString())).TotalDays;

                    monthlyWorkingDays += daysInbetween - nonWorkingDays;

                    monthlyPairsAccountedFor++;
                    nonWorkingDays = 0;
                }
            }

            return monthlyWorkingDays / monthlyPairsAccountedFor;
        }

        private Dictionary<DayOfWeek, double> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allrevenueRecords)
        {
            // can also provide a metrics for on average how many weekly sessions are complete 

            double averageSessions = CalculateAverageDailySessions(allrevenueRecords);

            decimal averageSessionPrice = allrevenueRecords.First().AverageSessionPrice;

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
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.First().AverageSessionPrice;
            if (allSessions == 0) return 0;

            return (double)allSessions / revenueRecords.Count;
        }

        private int GetEngagementMetrics(List<TrainerDailyRevenue> allRevenueRecords)
        {
            double averageMonthlySessions = 0;
            var monthlyPairsAccountedFor = 0;
            var totalMonthlyClientSessions = 0;

            var firstMonthlyRevenueRecord = allRevenueRecords.First();

            for(int i = 0; i < allRevenueRecords.Count; ++i)
            {
                var currentRecord = allRevenueRecords[i];

                // acculumate monthly sessions

                if (currentRecord.AsOfDate == firstMonthlyRevenueRecord.AsOfDate) 
                {
                    totalMonthlyClientSessions += currentRecord.TotalSessionsThisMonth;
                }
                // accumulation stopped - likely new month start
                else if(currentRecord.TotalSessionsThisMonth < allRevenueRecords[i - 1].TotalSessionsThisMonth)
                {
                    totalMonthlyClientSessions += currentRecord.TotalSessionsThisMonth;
                }
                // gather difference between current and previous record, in terms of totalSessionsThisMonth
                else
                {
                    totalMonthlyClientSessions += currentRecord.TotalSessionsThisMonth - allRevenueRecords[i - 1].TotalSessionsThisMonth;
                }

                // check if a months has passed from firstMonthlyRevenueRecord
                if (currentRecord.AsOfDate.Day == firstMonthlyRevenueRecord.AsOfDate.Day && currentRecord.AsOfDate.Month != firstMonthlyRevenueRecord.AsOfDate.Month)
                {
                    var totalActiveClients = currentRecord.ActiveClients;

                    averageMonthlySessions += totalMonthlyClientSessions / totalActiveClients;

                    monthlyPairsAccountedFor++;
                    totalMonthlyClientSessions = 0;
                    firstMonthlyRevenueRecord = currentRecord;
                }
            }

            return (int)Math.Round(averageMonthlySessions / monthlyPairsAccountedFor, 0);
        }
    }
}
