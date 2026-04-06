using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Models;
using ClientDashboard_API.Records;
using Quartz.Impl.Calendar;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.Services
{
    public class TrainerAnalyticsService(IUnitOfWork unitOfWork) : ITrainerAnalyticsService
    {
        

        public ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            var clientSessionData = GetTrainerBaseClientsAndAverageSessions(revenueRecords);

            var monthlySessions = GetBaseClientMonthlyAverageSessions(revenueRecords);

            var clientAcquisitionAndChurnData = CalculateMonthlyClientChangeRates(revenueRecords);

            return new ClientMetricsDto
            {
                BaseClients = clientSessionData.BaseClients,
                SessionsPerClient = clientSessionData.SessionsPerClient,
                AcquiredClients = clientAcquisitionAndChurnData.AcquiredClients,
                AcquisitionPercentage = clientAcquisitionAndChurnData.AcquisitionPercentage,
                ChurnedClients = clientAcquisitionAndChurnData.ChurnedClients,
                ChurnPercentage = clientAcquisitionAndChurnData.ChurnPercentage,
                NetGrowth = clientAcquisitionAndChurnData.NetGrowth,
                NetGrowthPercentage = clientAcquisitionAndChurnData.NetGrowthPercentage,
                MonthlyClientSessions = monthlySessions.MonthlyClientSessions
            };
        }

        public RevenuePatternsDto GetRevenuePatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            var monthlyWorkingDays = GetMonthlyWorkingDays(revenueRecords);

            var sessionPrice = Math.Round(CalculateAverageSessionPrice(revenueRecords));

            var averageRevenues = GetAverageDayWeekAndMonthRevenues(revenueRecords);

            return new RevenuePatternsDto
            {
                MonthlyWorkingDays = monthlyWorkingDays,
                SessionsPrice = sessionPrice,
                RevenuePerWorkingDay = averageRevenues.RevenuePerWorkingDay,
                RevenuePerWorkingWeek = averageRevenues.RevenuePerWorkingWeek,
                RevenuePerWorkingMonth = averageRevenues.RevenuePerWorkingMonth,
            };

        }

        public ActivityPatternsDto GetActivityPatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            var averageDailySessions = CalculateAverageDailySessions(revenueRecords);

            var weekdayMultiplierList = GetWeeklyActivityPatterns(revenueRecords, (int)averageDailySessions);

            var busiestWeekdays = weekdayMultiplierList.OrderByDescending(p => p.multiplier).Take(2).ToList();

            var lightestWeekdays = weekdayMultiplierList.OrderBy(p => p.multiplier).Take(2).ToList();

            return new ActivityPatternsDto { BusiestDays = busiestWeekdays, LightDays = lightestWeekdays };
        }

        private ClientMetricsDto GetTrainerBaseClientsAndAverageSessions(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var averageActiveClients = Math.Round(allRevenueRecords.Average(r => r.ActiveClients));

            // average daily sessions ?

            double averageSessionsPerClient = CalculateAverageClientSessions(allRevenueRecords, averageActiveClients);

            var statistics = new ClientMetricsDto
            {
                BaseClients = (int)averageActiveClients,
                SessionsPerClient = (int)averageSessionsPerClient
            };
            return statistics;
        }

        private ClientMetricsDto GetBaseClientMonthlyAverageSessions(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var monthsAccounterFor = 0;
            var totalSessions = 0;

            foreach(var record in allRevenueRecords)
            {
                var lastDayOfMonth = DateTime.DaysInMonth(record.AsOfDate.Year, record.AsOfDate.Month);

                if(record.AsOfDate.Day == lastDayOfMonth)
                {
                    monthsAccounterFor++;
                    totalSessions += record.TotalSessionsThisMonth;
                }
            }

            var averageMonthlySessions = Math.Round((double)totalSessions / monthsAccounterFor);

            return new ClientMetricsDto
            {
                MonthlyClientSessions = (int)averageMonthlySessions
            };

        }

        public CompleteTrainerAnalyticsDto GetAllAnalyticMetrics(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var clientMetrics = GetClientMetrics(allRevenueRecords);

            var revenuePatterns = GetRevenuePatterns(allRevenueRecords);

            var activityPatterns = GetActivityPatterns(allRevenueRecords);

            return new CompleteTrainerAnalyticsDto
            {
                BaseClients = clientMetrics.BaseClients,
                AcquiredClients = clientMetrics.AcquiredClients,
                AcquisitionPercentage = clientMetrics.AcquisitionPercentage,
                ChurnedClients = clientMetrics.ChurnedClients,
                ChurnPercentage = clientMetrics.ChurnPercentage,
                NetGrowth = clientMetrics.NetGrowth,
                NetGrowthPercentage = clientMetrics.NetGrowthPercentage,
                SessionsPerClient = clientMetrics.SessionsPerClient,
                MonthlyClientSessions = clientMetrics.MonthlyClientSessions,
                SessionsPrice = revenuePatterns.SessionsPrice,
                MonthlyWorkingDays = revenuePatterns.MonthlyWorkingDays,
                RevenuePerWorkingDay = revenuePatterns.RevenuePerWorkingDay,
                RevenuePerWorkingWeek = revenuePatterns.RevenuePerWorkingWeek,
                RevenuePerWorkingMonth = revenuePatterns.RevenuePerWorkingMonth,
                BusiestDays = activityPatterns.BusiestDays,
                LightDays = activityPatterns.LightDays
            };
        }



        // input of data can be last month / all records same outputs
        private ClientMetricsDto CalculateMonthlyClientChangeRates(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var firstRecord = allRevenueRecords.First();

            var startingMonthActiveClients = allRevenueRecords.First().ActiveClients;
            var monthsAccountedFor = 0;
            
            double churnCount = 0;
            double acquisitionCount = 0;

            double totalChurnedClients = 0;
            double totalAcquiredClients = 0;

            double churnRate = 0;
            double acquisitionRate = 0;

            for(int i = 0; i < allRevenueRecords.Count; i++)
            {
                // IMP currently not accounding for churn / acquisition into the new month 
                if(i != 0)
                {
                    // compares if the previous's active clients have increased / decreased comapred to the current records
                    if (allRevenueRecords[i - 1].ActiveClients < allRevenueRecords[i].ActiveClients)
                    {
                        acquisitionCount += allRevenueRecords[i].ActiveClients - allRevenueRecords[i - 1].ActiveClients;
                    }
                    else if (allRevenueRecords[i - 1].ActiveClients > allRevenueRecords[i].ActiveClients)
                    {
                        churnCount += allRevenueRecords[i - 1].ActiveClients - allRevenueRecords[i].ActiveClients;
                    }

                    // calculate the churn & acquisition rates and reset counts
                    var lastRecord = allRevenueRecords[allRevenueRecords.Count - 1];
                    // if last day of the month or last record - in the case of calculating for all data
                    if (allRevenueRecords[i].AsOfDate.Day == DateTime.DaysInMonth(firstRecord.AsOfDate.Year, firstRecord.AsOfDate.Month) || allRevenueRecords.Equals(lastRecord))
                    {
                        acquisitionRate += (acquisitionCount / startingMonthActiveClients) * 100;
                        churnRate += (churnCount / startingMonthActiveClients) * 100;
                        monthsAccountedFor++;

                        totalAcquiredClients += acquisitionCount;
                        totalChurnedClients += churnCount;

                        acquisitionCount = 0;
                        churnCount = 0;
                    }
                }
            }


            acquisitionRate = Math.Round(acquisitionRate / monthsAccountedFor);
            churnRate = Math.Round(churnRate / monthsAccountedFor);

            var averageAcquiredClients = Math.Round(totalAcquiredClients / monthsAccountedFor);
            var averageChurnedClients = Math.Round(totalChurnedClients / monthsAccountedFor);

            var netGrowth = (int)Math.Round(totalAcquiredClients - totalChurnedClients);
            var netGrowthPercentage = Math.Round(acquisitionRate - churnRate);

            return new ClientMetricsDto
            {
                AcquiredClients = (int)averageAcquiredClients,
                AcquisitionPercentage = acquisitionRate,
                ChurnedClients = (int)averageChurnedClients,
                ChurnPercentage = churnRate,
                NetGrowth = netGrowth,
                NetGrowthPercentage = netGrowthPercentage,
            };
        }

        private int GetMonthlyWorkingDays(List<TrainerDailyRevenue> allRevenueRecords)
        {
            // get working days from first record day to next month with that same record day
            var monthlyWorkingDays = 0;

            var monthsAccounterFor = 0;
            var nonWorkingDays = 0;

            var firstRecord = allRevenueRecords.First();

            foreach (var record in allRevenueRecords)
            {
                var totalDaysInMonth = DateTime.DaysInMonth(record.AsOfDate.Year, record.AsOfDate.Month);

                if (record.RevenueToday == 0)
                {
                    nonWorkingDays++;
                }
                // when reaching the last day of the month
                if (record.AsOfDate.Day == totalDaysInMonth)
                {
                  
                    monthlyWorkingDays +=  totalDaysInMonth - nonWorkingDays;

                    monthsAccounterFor++;
                    nonWorkingDays = 0;
                }
            }

            return monthlyWorkingDays / monthsAccounterFor;
        }

        private RevenuePatternsDto GetAverageDayWeekAndMonthRevenues(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var totalRevenue = 0m;

            var daysAccountedFor = 0;
            var weeksAccountedFor = 0;
            var monthsAccountedFor = 0;

            foreach (var record in allRevenueRecords)
            {
                var endOfWeek = DayOfWeek.Sunday;
                var lastDayOfMonth = DateTime.DaysInMonth(record.AsOfDate.Year, record.AsOfDate.Month);

                if (record.AsOfDate.Day == lastDayOfMonth)
                {
                    // end of month 
                    monthsAccountedFor++;
                }

                if (record.AsOfDate.DayOfWeek == endOfWeek)
                {
                    // end of week 
                    weeksAccountedFor++;
                }

                daysAccountedFor++;
                totalRevenue += record.RevenueToday;
            }

            var averageRevenuePerDay = Math.Round(totalRevenue / daysAccountedFor);
            var averageRevenuePerWeek = Math.Round(totalRevenue / weeksAccountedFor);
            var averageRevenuePerMonth = Math.Round(totalRevenue / monthsAccountedFor);

            return new RevenuePatternsDto
            {
                RevenuePerWorkingDay = (double)averageRevenuePerDay,
                RevenuePerWorkingWeek = (double)averageRevenuePerWeek,
                RevenuePerWorkingMonth = (double)averageRevenuePerMonth
            };
        }


        private List<WeeklyMultiplier> GetWeeklyActivityPatterns(List<TrainerDailyRevenue> allrevenueRecords, int averageClientSessions)
        {

            decimal averageSessionPrice = allrevenueRecords.First().AverageSessionPrice;

            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weekdayAverages = allrevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Average(r => (double)(r.RevenueToday / averageSessionPrice))
                );

            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
            var multipliers = new List<WeeklyMultiplier>();

            foreach (var day in weekdayAverages)
            {
                multipliers.Add(new WeeklyMultiplier(ReturnWeekdayEnumFromString(day.Key), Math.Round(day.Value / averageClientSessions, 1)));
            }

            return multipliers;
        }

        private double CalculateAverageDailySessions(List<TrainerDailyRevenue> revenueRecords)
        {
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.First().AverageSessionPrice;
            if (allSessions == 0) return 0;

            return Math.Round((double)allSessions / revenueRecords.Count);
        }

        private double CalculateAverageClientSessions(List<TrainerDailyRevenue> revenueRecords, double averageActiveClients)
        {
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.First().AverageSessionPrice;
            if (allSessions == 0) return 0;

            return Math.Round((double)allSessions / averageActiveClients);
        }

        private decimal CalculateAverageSessionPrice(List<TrainerDailyRevenue> revenueRecords)
        {
            return revenueRecords.Average(r => r.AverageSessionPrice);
        }

        private Weekdays ReturnWeekdayEnumFromString(DayOfWeek weekday)
        {
            switch (weekday)
            {
                case DayOfWeek.Monday:
                    return Weekdays.Mon;
                case DayOfWeek.Tuesday:
                    return Weekdays.Tue;
                case DayOfWeek.Wednesday:
                    return Weekdays.Wed;
                case DayOfWeek.Thursday:
                    return Weekdays.Thu;
                case DayOfWeek.Friday:
                    return Weekdays.Fri;
                case DayOfWeek.Saturday:
                    return Weekdays.Sat;
                case DayOfWeek.Sunday:
                    return Weekdays.Sun;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weekday), weekday, "Unsupported weekday");
            }
            ;
        }
    }
}
