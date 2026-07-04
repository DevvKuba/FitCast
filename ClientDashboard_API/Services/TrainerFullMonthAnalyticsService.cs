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
            var clientMetrics = GetClientMetrics(allRevenueRecords);

            var revenuePatterns = GetRevenuePatterns(allRevenueRecords);

            var activityPatterns = GetActivityPatterns(allRevenueRecords);

            var worktimeMetrics = GetWorktimeMetrics(allRevenueRecords);

            return new CompleteMonthTrainerAnalyticsDto
            {
                BaseClients = clientMetrics.BaseClients,
                AcquiredClients = clientMetrics.AcquiredClients,
                AcquisitionPercentage = clientMetrics.AcquisitionPercentage,
                ChurnedClients = clientMetrics.ChurnedClients,
                ChurnPercentage = clientMetrics.ChurnPercentage,
                NetGrowth = clientMetrics.NetGrowth,
                NetGrowthPercentage = clientMetrics.NetGrowthPercentage,
                AverageSessionsPerClient = clientMetrics.AverageSessionsPerClient,
                SessionsPrice = revenuePatterns.SessionsPrice,
                MonthlyWorkingDays = revenuePatterns.MonthlyWorkingDays,
                TotalClientSessions = clientMetrics.TotalClientSessions,
                TotalRevenue = revenuePatterns.TotalRevenue,
                RevenuePerWorkingDay = revenuePatterns.RevenuePerWorkingDay,
                RevenuePerWorkingWeek = revenuePatterns.RevenuePerWorkingWeek,
                TotalWorktimeMinutes = worktimeMetrics.TotalWorktimeMinutes,
                AverageDailyWorktime = worktimeMetrics.AverageDailyWorktime,
                AverageWeeklyWorktime = worktimeMetrics.AverageWeeklyWorktime,
                AllWeekdays = activityPatterns.AllWeekdays,
                BusiestDays = activityPatterns.BusiestDays,
                LightDays = activityPatterns.LightDays
            };
        }

        public ClientMetricsDto GetClientMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            var clientSessionData = GetTrainerBaseClientsAndAverageSessions(revenueRecords);

            var totalSessions = revenueRecords.Sum(r => r.SessionsToday);

            var clientAcquisitionAndChurnData = CalculateMonthlyClientChangeRates(revenueRecords);

            return new ClientMetricsDto
            {
                BaseClients = clientSessionData.BaseClients,
                AverageSessionsPerClient = clientSessionData.AverageSessionsPerClient,
                AcquiredClients = clientAcquisitionAndChurnData.AcquiredClients,
                AcquisitionPercentage = clientAcquisitionAndChurnData.AcquisitionPercentage,
                ChurnedClients = clientAcquisitionAndChurnData.ChurnedClients,
                ChurnPercentage = clientAcquisitionAndChurnData.ChurnPercentage,
                NetGrowth = clientAcquisitionAndChurnData.NetGrowth,
                NetGrowthPercentage = clientAcquisitionAndChurnData.NetGrowthPercentage,
                TotalClientSessions = totalSessions,
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

        public WorktimeMetricsDto GetWorktimeMetrics(List<TrainerDailyRevenue> revenueRecords)
        {
            var totalWorktimeMinutes = revenueRecords.Sum(r => r.TotalSessionDuration);

            var averageDailyWorktime = totalWorktimeMinutes / revenueRecords.Count();

            var fullWeeklyPeriods = (double)revenueRecords.Count / 7;

            var averageWeeklyWorktime = (int)Math.Round(totalWorktimeMinutes / fullWeeklyPeriods);

            return new WorktimeMetricsDto
            {
                TotalWorktimeMinutes = totalWorktimeMinutes,
                AverageDailyWorktime = averageDailyWorktime,
                AverageWeeklyWorktime = averageWeeklyWorktime,
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
                AverageSessionsPerClient = averageSessionsPerClient
            };
            return statistics;
        }

        private ClientMetricsDto CalculateMonthlyClientChangeRates(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var firstRecord = allRevenueRecords.First();

            var startingMonthActiveClients = allRevenueRecords.First().ActiveClients;
            
            int churnCount = 0;
            int acquisitionCount = 0;

            int totalChurnedClients = 0;
            int totalAcquiredClients = 0;

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


                    var lastDayOfMonth = DateTime.DaysInMonth(firstRecord.AsOfDate.Year, firstRecord.AsOfDate.Month);

                    if (allRevenueRecords[i].AsOfDate.Day == lastDayOfMonth)
                    {
                        acquisitionRate += (acquisitionCount / startingMonthActiveClients) * 100;
                        churnRate += (churnCount / startingMonthActiveClients) * 100;

                        totalAcquiredClients += acquisitionCount;
                        totalChurnedClients += churnCount;

                        acquisitionCount = 0;
                        churnCount = 0;
                    }
                }
            }

            var netGrowth = totalAcquiredClients - totalChurnedClients;
            var netGrowthPercentage = Math.Round(acquisitionRate - churnRate);

            return new ClientMetricsDto
            {
                AcquiredClients = totalAcquiredClients,
                AcquisitionPercentage = acquisitionRate,
                ChurnedClients = totalChurnedClients,
                ChurnPercentage = churnRate,
                NetGrowth = netGrowth,
                NetGrowthPercentage = netGrowthPercentage,
            };
        }

        private RevenuePatternsDto GetAverageDayWeekAndMonthRevenues(List<TrainerDailyRevenue> allRevenueRecords)
        {
            var totalRevenue = allRevenueRecords.Sum(r => r.RevenueToday);

            var averageRevenuePerDay = Math.Round(totalRevenue / allRevenueRecords.Count);

            var fullWeeklyPeriods = allRevenueRecords.Count / 7;

            var averageRevenuePerWeek = Math.Round(totalRevenue / fullWeeklyPeriods);

            return new RevenuePatternsDto
            {
                TotalRevenue = totalRevenue,
                RevenuePerWorkingDay = averageRevenuePerDay,
                RevenuePerWorkingWeek = averageRevenuePerWeek,
            };
        }

        private int CalculateAverageClientSessions(List<TrainerDailyRevenue> revenueRecords, int averageActiveClients)
        {
            var allSessions = revenueRecords.Sum(r => r.SessionsToday);

            if (allSessions == 0) return 0;

            return allSessions / averageActiveClients;
        }
}
