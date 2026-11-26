using ClientDashboard_API.Helpers;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class WorkoutEditWindowHelperTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public void GetEarliestEditableDate_KeepsThePreviousMonthOpenDuringTheGraceDays(int day)
        {
            Assert.Equal(new DateOnly(2026, 10, 1), WorkoutEditWindowHelper.GetEarliestEditableDate(new DateOnly(2026, 11, day)));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(30)]
        public void GetEarliestEditableDate_StartsAtTheCurrentMonthAfterTheGraceDays(int day)
        {
            Assert.Equal(new DateOnly(2026, 11, 1), WorkoutEditWindowHelper.GetEarliestEditableDate(new DateOnly(2026, 11, day)));
        }

        [Fact]
        public void GetEarliestEditableDate_OpensDecemberOfTheYearBeforeInEarlyJanuary()
        {
            Assert.Equal(new DateOnly(2026, 12, 1), WorkoutEditWindowHelper.GetEarliestEditableDate(new DateOnly(2027, 1, 3)));
        }

        [Fact]
        public void IsLocked_LocksThePreviousMonthOnceTheGraceDaysPass()
        {
            var lastDayOfOctober = new DateOnly(2026, 10, 31);

            Assert.False(WorkoutEditWindowHelper.IsLocked(lastDayOfOctober, new DateOnly(2026, 11, 7)));
            Assert.True(WorkoutEditWindowHelper.IsLocked(lastDayOfOctober, new DateOnly(2026, 11, 8)));
        }

        [Fact]
        public void IsLocked_NeverLocksTheCurrentMonth()
        {
            Assert.False(WorkoutEditWindowHelper.IsLocked(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)));
        }
    }
}
