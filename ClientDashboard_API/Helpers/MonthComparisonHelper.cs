using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Helpers
{
    public static class MonthComparisonHelper
    {
        // A past month against the whole month before it. Null when the month before has no snapshots.
        public static MonthComparisonDto? CompareWithPreviousMonth(List<TrainerDailyRevenue> monthRecords, List<TrainerDailyRevenue> previousMonthRecords)
        {
            if (monthRecords.Count == 0 || previousMonthRecords.Count == 0)
            {
                return null;
            }

            var previousMonthDate = previousMonthRecords.First().AsOfDate;
            var previousMonthDays = DateTime.DaysInMonth(previousMonthDate.Year, previousMonthDate.Month);

            return BuildComparison(monthRecords, previousMonthRecords, previousMonthDays);
        }

        // The current month so far against the same days of last month, e.g. the 1st-9th against the 1st-9th
        public static MonthComparisonDto? CompareMonthToDate(List<TrainerDailyRevenue> currentMonthRecords, List<TrainerDailyRevenue> previousMonthRecords)
        {
            if (currentMonthRecords.Count == 0)
            {
                return null;
            }

            var lastRecordedDay = currentMonthRecords.Max(r => r.AsOfDate.Day);
            var previousMonthToDate = previousMonthRecords.Where(r => r.AsOfDate.Day <= lastRecordedDay).ToList();

            if (previousMonthToDate.Count == 0)
            {
                return null;
            }

            return BuildComparison(currentMonthRecords, previousMonthToDate, lastRecordedDay);
        }

        // Day-to-day changes in active clients: a rise counts as acquisitions, a drop as churn
        public static (int Acquired, int Churned) CountClientChanges(List<TrainerDailyRevenue> revenueRecords)
        {
            var orderedRecords = revenueRecords.OrderBy(r => r.AsOfDate).ToList();

            int acquired = 0;
            int churned = 0;

            for (int i = 1; i < orderedRecords.Count; i++)
            {
                var change = orderedRecords[i].ActiveClients - orderedRecords[i - 1].ActiveClients;

                if (change > 0)
                {
                    acquired += change;
                }
                else if (change < 0)
                {
                    churned -= change;
                }
            }

            return (acquired, churned);
        }

        private static MonthComparisonDto BuildComparison(List<TrainerDailyRevenue> records, List<TrainerDailyRevenue> comparedRecords, int expectedComparedDays)
        {
            var clientChanges = CountClientChanges(records);
            var comparedClientChanges = CountClientChanges(comparedRecords);

            return new MonthComparisonDto
            {
                BaseClientsChange = GetBaseClients(records) - GetBaseClients(comparedRecords),
                SessionsChange = records.Sum(r => r.SessionsToday) - comparedRecords.Sum(r => r.SessionsToday),
                EarnedRevenueChange = records.Sum(r => r.RevenueToday) - comparedRecords.Sum(r => r.RevenueToday),
                WorktimeMinutesChange = records.Sum(r => r.TotalSessionDuration) - comparedRecords.Sum(r => r.TotalSessionDuration),
                AcquiredClientsChange = clientChanges.Acquired - comparedClientChanges.Acquired,
                ChurnedClientsChange = clientChanges.Churned - comparedClientChanges.Churned,
                ComparedMonthIsPartial = comparedRecords.Count < expectedComparedDays
            };
        }

        private static int GetBaseClients(List<TrainerDailyRevenue> records)
        {
            return (int)Math.Round(records.Average(r => r.ActiveClients));
        }
    }
}
