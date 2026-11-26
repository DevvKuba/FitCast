using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class DailyRevenueSnapshotHelperTests
    {
        [Fact]
        public void RefreshFromWorkouts_RebuildsTheDaysFiguresWithItsRecordedPrice()
        {
            var snapshot = Snapshot(new DateOnly(2026, 11, 10), sessionPrice: 40m);
            var workouts = new List<Workout> { WorkoutOn(new DateOnly(2026, 11, 10), 60), WorkoutOn(new DateOnly(2026, 11, 10), 45) };

            DailyRevenueSnapshotHelper.RefreshFromWorkouts([snapshot], workouts);

            Assert.Equal(2, snapshot.SessionsToday);
            Assert.Equal(80m, snapshot.RevenueToday);
            Assert.Equal(105, snapshot.TotalSessionDuration);
        }

        [Fact]
        public void RefreshFromWorkouts_CountsMonthToDateWorkoutsOnDaysWithoutASnapshot()
        {
            // The 3rd has no snapshot, so its workout is priced at the refreshed snapshot's price
            var snapshot = Snapshot(new DateOnly(2026, 11, 5), sessionPrice: 50m);
            var workouts = new List<Workout> { WorkoutOn(new DateOnly(2026, 11, 3), 60), WorkoutOn(new DateOnly(2026, 11, 5), 60) };

            DailyRevenueSnapshotHelper.RefreshFromWorkouts([snapshot], workouts);

            Assert.Equal(2, snapshot.TotalSessionsThisMonth);
            Assert.Equal(100m, snapshot.MonthlyRevenueThusFar);
        }

        [Fact]
        public void RefreshFromWorkouts_PricesEachDayOfTheMonthToDateAtItsOwnPrice()
        {
            var earlier = Snapshot(new DateOnly(2026, 11, 2), sessionPrice: 40m);
            var later = Snapshot(new DateOnly(2026, 11, 20), sessionPrice: 60m);
            var workouts = new List<Workout> { WorkoutOn(new DateOnly(2026, 11, 2), 60), WorkoutOn(new DateOnly(2026, 11, 20), 60) };

            DailyRevenueSnapshotHelper.RefreshFromWorkouts([earlier, later], workouts);

            Assert.Equal(100m, later.MonthlyRevenueThusFar);
        }

        [Fact]
        public void RefreshFromWorkouts_RestartsTheMonthToDateTotalsInANewMonth()
        {
            var october = Snapshot(new DateOnly(2026, 10, 31), sessionPrice: 50m);
            var november = Snapshot(new DateOnly(2026, 11, 1), sessionPrice: 50m);
            var workouts = new List<Workout> { WorkoutOn(new DateOnly(2026, 10, 31), 60), WorkoutOn(new DateOnly(2026, 11, 1), 60) };

            DailyRevenueSnapshotHelper.RefreshFromWorkouts([october, november], workouts);

            Assert.Equal(1, november.TotalSessionsThisMonth);
            Assert.Equal(50m, november.MonthlyRevenueThusFar);
        }

        [Fact]
        public void RefreshFromWorkouts_ClearsADayWhoseWorkoutsWereRemoved()
        {
            var snapshot = Snapshot(new DateOnly(2026, 11, 10), sessionPrice: 50m);
            snapshot.SessionsToday = 3;
            snapshot.RevenueToday = 150m;

            DailyRevenueSnapshotHelper.RefreshFromWorkouts([snapshot], []);

            Assert.Equal(0, snapshot.SessionsToday);
            Assert.Equal(0m, snapshot.RevenueToday);
        }

        private static TrainerDailyRevenue Snapshot(DateOnly date, decimal sessionPrice) => new()
        {
            TrainerId = 1,
            AsOfDate = date,
            AverageSessionPrice = sessionPrice,
            ActiveClients = 5
        };

        private static Workout WorkoutOn(DateOnly date, int duration) => new()
        {
            ClientName = "rob",
            WorkoutTitle = "session",
            SessionDate = date,
            Duration = duration
        };
    }
}
