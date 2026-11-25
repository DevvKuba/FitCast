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
        public void GetSessionDate_UsesTheStartTimesLocalDate()
        {
            // 23:30 UTC on the 9th is already the 10th in Auckland
            var start = new DateTime(2026, 11, 9, 23, 30, 0, DateTimeKind.Utc);

            Assert.Equal(new DateOnly(2026, 11, 10), WorkoutSessionTimeHelper.GetSessionDate(start, Auckland));
        }

        [Fact]
        public void GetSessionDate_FollowsDaylightSavingInTheTrainersZone()
        {
            // 23:30 UTC on 18 June is 00:30 on the 19th in London (UTC+1)
            var start = new DateTime(2026, 6, 18, 23, 30, 0, DateTimeKind.Utc);

            Assert.Equal(new DateOnly(2026, 6, 19), WorkoutSessionTimeHelper.GetSessionDate(start, London));
        }

        [Fact]
        public void IsInFuture_ComparesTheStartWithNow()
        {
            var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(WorkoutSessionTimeHelper.IsInFuture(now.AddMinutes(1), now));
            Assert.False(WorkoutSessionTimeHelper.IsInFuture(now.AddMinutes(-1), now));
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
