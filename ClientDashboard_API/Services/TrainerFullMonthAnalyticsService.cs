using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Records;

namespace ClientDashboard_API.Services
{
    public class TrainerFullMonthAnalyticsService : ITrainerFullMonthAnalyticsService
    {
        public CompleteMonthTrainerAnalyticsDto GetAllAnalyticMetrics(List<TrainerDailyRevenue> allRevenueRecords)
        {
            // check here

            var clientMetrics = GetClientMetrics(allRevenueRecords);

            var revenuePatterns = GetRevenuePatterns(allRevenueRecords);

            var activityPatterns = GetActivityPatterns(allRevenueRecords);

            return new CompleteMonthTrainerAnalyticsDto
            {
                BaseClients = clientMetrics.BaseClients,
                AcquiredClients = clientMetrics.AcquiredClients,
                AcquisitionPercentage = clientMetrics.AcquisitionPercentage,
                ChurnedClients = clientMetrics.ChurnedClients,
                ChurnPercentage = clientMetrics.ChurnPercentage,
                NetGrowth = clientMetrics.NetGrowth,
                NetGrowthPercentage = clientMetrics.NetGrowthPercentage,
                SessionsPerClient = clientMetrics.SessionsPerClient,
                AverageClientSessions = clientMetrics.AverageClientSessions,
                SessionsPrice = revenuePatterns.SessionsPrice,
                MonthlyWorkingDays = revenuePatterns.MonthlyWorkingDays,
                TotalClientSessions = clientMetrics.TotalClientSessions,
                TotalRevenue = revenuePatterns.TotalRevenue,
                RevenuePerWorkingDay = revenuePatterns.RevenuePerWorkingDay,
                RevenuePerWorkingWeek = revenuePatterns.RevenuePerWorkingWeek,
                AllWeekdays = activityPatterns.AllWeekdays,
                BusiestDays = activityPatterns.BusiestDays,
                LightDays = activityPatterns.LightDays
            };
        }

        public ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            var clientSessionData = GetTrainerBaseClientsAndAverageSessions(revenueRecords);

            var totalSessions = revenueRecords.Sum(r => r.SessionsToday);

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
                TotalClientSessions = totalSessions,
                AverageClientSessions = monthlySessions.AverageClientSessions
            };
        }

        public RevenuePatternsDto GetRevenuePatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            var monthlyWorkingDays = revenueRecords.Count(r =>
            {
                if (r.SessionsToday > 0) return true;
                return false;
            });

            var sessionPrice = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice));

            var averageRevenues = GetAverageDayWeekAndMonthRevenues(revenueRecords);

            return new RevenuePatternsDto
            {
                MonthlyWorkingDays = monthlyWorkingDays,
                SessionsPrice = sessionPrice,
                TotalRevenue = averageRevenues.TotalRevenue,
                RevenuePerWorkingDay = averageRevenues.RevenuePerWorkingDay,
                RevenuePerWorkingWeek = averageRevenues.RevenuePerWorkingWeek,
            };

        }

        public ActivityPatternsDto GetActivityPatterns(List<TrainerDailyRevenue> revenueRecords)
        {
            var averageDailySessions = revenueRecords.Average(r => r.SessionsToday);

            var weekdayMultiplierList = GetWeeklyActivityPatterns(revenueRecords, (int)averageDailySessions);

            var busiestWeekdays = weekdayMultiplierList.OrderByDescending(p => p.multiplier).Take(2).ToList();

            var lightestWeekdays = weekdayMultiplierList.OrderBy(p => p.multiplier).Take(2).ToList();

            return new ActivityPatternsDto { AllWeekdays = weekdayMultiplierList, BusiestDays = busiestWeekdays, LightDays = lightestWeekdays };
        }

        private ClientMetricsDto GetTrainerBaseClientsAndAverageSessions(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var averageActiveClients = (int)Math.Round(allRevenueRecords.Average(r => r.ActiveClients));
            
            var averageSessionsPerClient = CalculateAverageClientSessions(allRevenueRecords, averageActiveClients);

            var statistics = new ClientMetricsDto
            {
                BaseClients = averageActiveClients,
                SessionsPerClient = averageSessionsPerClient
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
                AverageClientSessions = (int)averageMonthlySessions
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
                    if (allRevenueRecords[i].AsOfDate.Day == DateTime.DaysInMonth(firstRecord.AsOfDate.Year, firstRecord.AsOfDate.Month))
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
            if (monthsAccountedFor == 0)
            {
                throw new InvalidOperationException("Analytics expected at least one full month, but none were found.");
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

        private RevenuePatternsDto GetAverageDayWeekAndMonthRevenues(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var totalRevenue = allRevenueRecords.Sum(r => r.RevenueToday);

            var endOfWeek = DayOfWeek.Sunday;

            var averageRevenuePerDay = Math.Round(totalRevenue / allRevenueRecords.Count);

            var averageRevenuePerWeek = Math.Round(totalRevenue / allRevenueRecords.Count(r =>
            {
                if (r.AsOfDate.DayOfWeek == endOfWeek) return true;
                return false;
            }));

            return new RevenuePatternsDto
            {
                TotalRevenue = totalRevenue,
                RevenuePerWorkingDay = (double)averageRevenuePerDay,
                RevenuePerWorkingWeek = (double)averageRevenuePerWeek,
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
                g => g.Average(r => (r.SessionsToday))
                );

            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
            var multipliers = new List<WeeklyMultiplier>();

            foreach (var day in weekdayAverages)
            {
                multipliers.Add(new WeeklyMultiplier(ReturnWeekdayEnumFromString(day.Key), Math.Round(day.Value / averageClientSessions, 1)));
            }

            return multipliers;
        }

        private int CalculateAverageClientSessions(List<TrainerDailyRevenue> revenueRecords, int averageActiveClients)
        {
            var allSessions = revenueRecords.Sum(r => r.SessionsToday);

            if (allSessions == 0) return 0;

            return allSessions / averageActiveClients;
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
