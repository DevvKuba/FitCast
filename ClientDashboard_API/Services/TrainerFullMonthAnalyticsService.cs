using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Services;
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
            var monthlyWorkingDays = CountWorkingDays(revenueRecords);

            var sessionPrice = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice), 2);

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

            var workingDays = CountWorkingDays(revenueRecords);
            var averageDailyWorktime = workingDays == 0 ? 0 : (int)Math.Round((double)totalWorktimeMinutes / workingDays);

            var weeksCovered = revenueRecords.Count / 7.0;
            var averageWeeklyWorktime = (int)Math.Round(totalWorktimeMinutes / weeksCovered);

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

            var weekdayMultiplierList = WeekdayActivityPatternHelper.GetWeeklyActivityPatterns(revenueRecords, averageDailySessions);

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
            var orderedRecords = allRevenueRecords.OrderBy(r => r.AsOfDate).ToList();

            var startingMonthActiveClients = orderedRecords.First().ActiveClients;

            int totalChurnedClients = 0;
            int totalAcquiredClients = 0;

            // day-to-day changes in active clients: a rise counts as acquisitions, a drop as churn
            for (int i = 1; i < orderedRecords.Count; i++)
            {
                var change = orderedRecords[i].ActiveClients - orderedRecords[i - 1].ActiveClients;

                if (change > 0)
                {
                    totalAcquiredClients += change;
                }
                else if (change < 0)
                {
                    totalChurnedClients -= change;
                }
            }

            var acquisitionRate = CalculatePercentageOf(totalAcquiredClients, startingMonthActiveClients);
            var churnRate = CalculatePercentageOf(totalChurnedClients, startingMonthActiveClients);

            var netGrowth = totalAcquiredClients - totalChurnedClients;
            var netGrowthPercentage = Math.Round(acquisitionRate - churnRate, 1);

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

            var workingDays = CountWorkingDays(allRevenueRecords);
            var averageRevenuePerDay = workingDays == 0 ? 0m : Math.Round(totalRevenue / workingDays, 2);

            // weeks covered by the recorded days, as a decimal so a 30 day month is ~4.3 weeks rather than 4
            var weeksCovered = allRevenueRecords.Count / 7m;
            var averageRevenuePerWeek = Math.Round(totalRevenue / weeksCovered, 2);

            return new RevenuePatternsDto
            {
                TotalRevenue = totalRevenue,
                RevenuePerWorkingDay = averageRevenuePerDay,
                RevenuePerWorkingWeek = averageRevenuePerWeek,
            };
        }

        private double CalculateAverageClientSessions(List<TrainerDailyRevenue> revenueRecords, int averageActiveClients)
        {
            var allSessions = revenueRecords.Sum(r => r.SessionsToday);

            if (allSessions == 0 || averageActiveClients == 0) return 0;

            return Math.Round((double)allSessions / averageActiveClients, 1);
        }

        private static int CountWorkingDays(List<TrainerDailyRevenue> revenueRecords)
        {
            return revenueRecords.Count(r => r.SessionsToday > 0);
        }

        private static double CalculatePercentageOf(int count, int total)
        {
            if (total == 0) return 0;

            return Math.Round((double)count / total * 100, 1);
        }
    }
}
