using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class WorkoutSessionHelperTests
    {
        private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.London);
        private static readonly TimeZoneInfo Auckland = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.Auckland);

        [Theory]
        [InlineData(null, 60)]     // no recent workouts
        [InlineData(43.0, 45)]     // rounded to the nearest 5 minutes
        [InlineData(47.6, 50)]
        [InlineData(1.0, 5)]       // never shorter than 5 minutes
        public void QuickAddGetSessionMinutes_RoundsTheAverageOrFallsBackToTheDefault(double? averageMinutes, int expectedMinutes)
        {
            Assert.Equal(expectedMinutes, QuickAddSessionHelper.GetSessionMinutes(averageMinutes));
        }

        [Theory]
        [InlineData(9, 14, 9, 0)]
        [InlineData(9, 15, 9, 15)]
        [InlineData(9, 59, 9, 45)]
        public void QuickAddGetSessionEndUtc_RoundsDownToTheQuarterHour(int hour, int minute, int expectedHour, int expectedMinute)
        {
            var now = new DateTime(2026, 11, 10, hour, minute, 37, DateTimeKind.Utc);

            var end = QuickAddSessionHelper.GetSessionEndUtc(now);

            Assert.Equal(new DateTime(2026, 11, 10, expectedHour, expectedMinute, 0, DateTimeKind.Utc), end);
        }

        [Fact]
        public void GetManualStartUtc_UsesTheRequestedStartWhenGiven()
        {
            var requested = new DateTime(2026, 11, 10, 7, 30, 0, DateTimeKind.Utc);

            Assert.Equal(requested, WorkoutSessionTimeHelper.GetManualStartUtc(requested, new DateOnly(2026, 11, 10), London));
        }

        [Fact]
        public void GetManualStartUtc_DefaultsToNoonInTheTrainersZone()
        {
            var start = WorkoutSessionTimeHelper.GetManualStartUtc(null, new DateOnly(2026, 6, 19), London);

            Assert.Equal(new DateTime(2026, 6, 19, 11, 0, 0, DateTimeKind.Utc), start); // London is UTC+1 in June
        }

        [Fact]
        public void GetUpdatedStartUtc_PrefersTheRequestedStart()
        {
            var requested = new DateTime(2026, 11, 12, 14, 0, 0, DateTimeKind.Utc);
            var existing = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

            Assert.Equal(requested, WorkoutSessionTimeHelper.GetUpdatedStartUtc(requested, existing, new DateOnly(2026, 11, 12), London));
        }

        [Fact]
        public void GetUpdatedStartUtc_KeepsTheExistingClockTimeOnTheNewDate()
        {
            // 09:00 London in November (UTC+0) moved into June (UTC+1) stays 09:00 local, so 08:00 UTC
            var existing = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

            var moved = WorkoutSessionTimeHelper.GetUpdatedStartUtc(null, existing, new DateOnly(2026, 6, 19), London);

            Assert.Equal(new DateTime(2026, 6, 19, 8, 0, 0, DateTimeKind.Utc), moved);
        }

        [Fact]
        public void GetUpdatedStartUtc_StaysNullForWorkoutsWithoutTimes()
        {
            Assert.Null(WorkoutSessionTimeHelper.GetUpdatedStartUtc(null, null, new DateOnly(2026, 11, 12), London));
        }

        [Fact]
        public void GetSessionDate_UsesTheStartTimesLocalDate()
        {
            // 23:30 UTC on the 9th is already the 10th in Auckland
            var start = new DateTime(2026, 11, 9, 23, 30, 0, DateTimeKind.Utc);

            Assert.Equal(new DateOnly(2026, 11, 10), WorkoutSessionTimeHelper.GetSessionDate(start, "01/01/2020", Auckland));
        }

        [Fact]
        public void GetSessionDate_FallsBackToTheRequestedDate()
        {
            Assert.Equal(new DateOnly(2026, 11, 10), WorkoutSessionTimeHelper.GetSessionDate(null, "10/11/2026", London));
        }

        [Fact]
        public void IsInFuture_ChecksTheTimeWhenAStartIsKnown()
        {
            var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(WorkoutSessionTimeHelper.IsInFuture(now.AddMinutes(1), new DateOnly(2026, 11, 10), London, now));
            Assert.False(WorkoutSessionTimeHelper.IsInFuture(now.AddMinutes(-1), new DateOnly(2026, 11, 10), London, now));
        }

        [Fact]
        public void IsInFuture_ChecksOnlyTheDateWithoutAStart()
        {
            var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

            Assert.False(WorkoutSessionTimeHelper.IsInFuture(null, new DateOnly(2026, 11, 10), London, now));
            Assert.True(WorkoutSessionTimeHelper.IsInFuture(null, new DateOnly(2026, 11, 11), London, now));
        }

        [Fact]
        public void TrainerLocalDate_TodayAndYesterdayFollowTheTrainersZone()
        {
            // 11:00 UTC on the 10th is 00:00 on the 11th in Auckland (UTC+13 in November)
            var now = new DateTime(2026, 11, 10, 11, 0, 0, DateTimeKind.Utc);

            Assert.Equal(new DateOnly(2026, 11, 11), TrainerLocalDateHelper.GetToday(SupportedTimeZones.Auckland, now));
            Assert.Equal(new DateOnly(2026, 11, 10), TrainerLocalDateHelper.GetYesterday(SupportedTimeZones.Auckland, now));
            Assert.Equal(new DateOnly(2026, 11, 9), TrainerLocalDateHelper.GetYesterday(SupportedTimeZones.London, now));
        }
    }
}
