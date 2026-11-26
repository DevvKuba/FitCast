using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class CurrentMonthRecordsHelperTests
    {
        private static readonly DateOnly FirstDay = new(2026, 11, 1);

        [Fact]
        public void BuildLiveRecords_AddsOneRecordPerDayUpToToday()
        {
            var records = CurrentMonthRecordsHelper.BuildLiveRecords(1, FirstDay, new DateOnly(2026, 11, 5), [], [], currentActiveClients: 3, sessionPrice: 50m);

            Assert.Equal(5, records.Count);
            Assert.Equal(new DateOnly(2026, 11, 5), records[^1].AsOfDate);
        }

        [Fact]
        public void BuildLiveRecords_IncludesWorkoutsLoggedToday()
        {
            var today = new DateOnly(2026, 11, 5);
            var workouts = new List<Workout> { WorkoutOn(today, 60), WorkoutOn(today, 30) };

            var todayRecord = CurrentMonthRecordsHelper.BuildLiveRecords(1, FirstDay, today, workouts, [], currentActiveClients: 3, sessionPrice: 50m)[^1];

            Assert.Equal(2, todayRecord.SessionsToday);
            Assert.Equal(100m, todayRecord.RevenueToday);
            Assert.Equal(90, todayRecord.TotalSessionDuration);
            Assert.Equal(3, todayRecord.ActiveClients);
        }

        [Fact]
        public void BuildLiveRecords_KeepsARecordedDaysPriceAndActiveClients()
        {
            var recordedDay = new DateOnly(2026, 11, 2);
            var snapshot = new TrainerDailyRevenue { TrainerId = 1, AsOfDate = recordedDay, AverageSessionPrice = 40m, ActiveClients = 7 };
            var workouts = new List<Workout> { WorkoutOn(recordedDay, 60) };

            var records = CurrentMonthRecordsHelper.BuildLiveRecords(1, FirstDay, new DateOnly(2026, 11, 3), workouts, [snapshot], currentActiveClients: 3, sessionPrice: 50m);

            var recorded = records.Single(r => r.AsOfDate == recordedDay);
            Assert.Equal(40m, recorded.RevenueToday);
            Assert.Equal(7, recorded.ActiveClients);
        }

        private static Workout WorkoutOn(DateOnly date, int duration) => new()
        {
            ClientName = "rob",
            WorkoutTitle = "session",
            SessionDate = date,
            Duration = duration
        };
    }
}
