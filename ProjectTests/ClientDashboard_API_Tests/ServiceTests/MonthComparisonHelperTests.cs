using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class MonthComparisonHelperTests
    {
        [Fact]
        public void CompareWithPreviousMonth_ReturnsTheDifferenceForEachFigure()
        {
            var october = Month(2026, 10, days: 31, activeClients: 10, sessions: 2, revenue: 100m, minutes: 120);
            var november = Month(2026, 11, days: 30, activeClients: 12, sessions: 3, revenue: 150m, minutes: 180);

            var comparison = MonthComparisonHelper.CompareWithPreviousMonth(november, october);

            Assert.NotNull(comparison);
            Assert.Equal(2, comparison.BaseClientsChange);
            Assert.Equal(90 - 62, comparison.SessionsChange);
            Assert.Equal(4500m - 3100m, comparison.EarnedRevenueChange);
            Assert.Equal(5400 - 3720, comparison.WorktimeMinutesChange);
            Assert.False(comparison.ComparedMonthIsPartial);
        }

        [Fact]
        public void CompareWithPreviousMonth_FlagsAPartialPreviousMonth()
        {
            var october = Month(2026, 10, days: 12, activeClients: 10, sessions: 2, revenue: 100m, minutes: 60);
            var november = Month(2026, 11, days: 30, activeClients: 10, sessions: 2, revenue: 100m, minutes: 60);

            var comparison = MonthComparisonHelper.CompareWithPreviousMonth(november, october);

            Assert.True(comparison!.ComparedMonthIsPartial);
        }

        [Fact]
        public void CompareWithPreviousMonth_ReturnsNullWithoutAPreviousMonth()
        {
            var november = Month(2026, 11, days: 30, activeClients: 10, sessions: 2, revenue: 100m, minutes: 60);

            Assert.Null(MonthComparisonHelper.CompareWithPreviousMonth(november, []));
        }

        [Fact]
        public void CompareMonthToDate_OnlyComparesTheSameDaysOfLastMonth()
        {
            // 9 days recorded this month against a full last month: only last month's 1st-9th count
            var october = Month(2026, 10, days: 31, activeClients: 10, sessions: 1, revenue: 50m, minutes: 60);
            var november = Month(2026, 11, days: 9, activeClients: 10, sessions: 2, revenue: 100m, minutes: 60);

            var comparison = MonthComparisonHelper.CompareMonthToDate(november, october);

            Assert.Equal(18 - 9, comparison!.SessionsChange);
            Assert.Equal(900m - 450m, comparison.EarnedRevenueChange);
            Assert.False(comparison.ComparedMonthIsPartial);
        }

        [Fact]
        public void CompareMonthToDate_ReturnsNullWithoutLastMonthsDays()
        {
            var november = Month(2026, 11, days: 9, activeClients: 10, sessions: 2, revenue: 100m, minutes: 60);

            Assert.Null(MonthComparisonHelper.CompareMonthToDate(november, []));
        }

        [Fact]
        public void CountClientChanges_CountsRisesAsAcquisitionsAndDropsAsChurn()
        {
            var records = Month(2026, 11, days: 5, activeClients: 10, sessions: 1, revenue: 50m, minutes: 60);
            records[1].ActiveClients = 12; // +2
            records[2].ActiveClients = 11; // -1
            records[3].ActiveClients = 11;
            records[4].ActiveClients = 14; // +3

            var (acquired, churned) = MonthComparisonHelper.CountClientChanges(records);

            Assert.Equal(5, acquired);
            Assert.Equal(1, churned);
        }

        private static List<TrainerDailyRevenue> Month(int year, int month, int days, int activeClients, int sessions, decimal revenue, int minutes)
        {
            var records = new List<TrainerDailyRevenue>();
            for (int day = 1; day <= days; day++)
            {
                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = 1,
                    AsOfDate = new DateOnly(year, month, day),
                    ActiveClients = activeClients,
                    SessionsToday = sessions,
                    RevenueToday = revenue,
                    TotalSessionDuration = minutes
                });
            }
            return records;
        }
    }
}
