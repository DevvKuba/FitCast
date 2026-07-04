using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using FluentAssertions;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class WeekdayActivityPatternHelperTests
    {
        // 2024-01-01 is a Monday, so dates below map to known weekdays:
        // 01 Mon, 02 Tue, 03 Wed, 04 Thu, 05 Fri, 06 Sat, 07 Sun, 08 Mon (following week)

        [Fact]
        public void GetNumberOfSessionsForWeekdays_SumsSessionsPerWeekday()
        {
            // Arrange - two Mondays accumulate; a zero-session Wednesday is still present
            var records = new List<TrainerDailyRevenue>
            {
                Record(new DateOnly(2024, 1, 1), sessions: 3), // Mon
                Record(new DateOnly(2024, 1, 8), sessions: 2), // Mon (following week)
                Record(new DateOnly(2024, 1, 2), sessions: 4), // Tue
                Record(new DateOnly(2024, 1, 3), sessions: 0), // Wed
            };

            // Act
            var result = WeekdayActivityPatternHelper.GetNumberOfSessionsForWeekdays(records);

            // Assert
            result.Should().HaveCount(3);
            result.Single(r => r.day == Weekdays.Mon).totalSessions.Should().Be(5);
            result.Single(r => r.day == Weekdays.Tue).totalSessions.Should().Be(4);
            result.Single(r => r.day == Weekdays.Wed).totalSessions.Should().Be(0);
        }

        [Fact]
        public void GetNumberOfSessionsForWeekdays_OnlyIncludesWeekdaysPresentInData()
        {
            // Arrange - only Mondays and Fridays appear
            var records = new List<TrainerDailyRevenue>
            {
                Record(new DateOnly(2024, 1, 1), sessions: 1), // Mon
                Record(new DateOnly(2024, 1, 5), sessions: 2), // Fri
            };

            // Act
            var result = WeekdayActivityPatternHelper.GetNumberOfSessionsForWeekdays(records);

            // Assert - weekdays with no records are absent (not zero-filled by the helper)
            result.Select(r => r.day).Should().BeEquivalentTo(new[] { Weekdays.Mon, Weekdays.Fri });
        }

        [Fact]
        public void GetWeeklyActivityPatterns_CalculatesTotalsAndMultipliers()
        {
            // Arrange - averageClientSessions = 2
            // Mon: sessions 4 and 2 -> sum 6, avg 3 -> multiplier 3/2 = 1.5
            // Tue: sessions 2       -> sum 2, avg 2 -> multiplier 2/2 = 1.0
            var records = new List<TrainerDailyRevenue>
            {
                Record(new DateOnly(2024, 1, 1), sessions: 4), // Mon
                Record(new DateOnly(2024, 1, 8), sessions: 2), // Mon
                Record(new DateOnly(2024, 1, 2), sessions: 2), // Tue
            };

            // Act
            var result = WeekdayActivityPatternHelper.GetWeeklyActivityPatterns(records, averageClientSessions: 2);

            // Assert
            var monday = result.Single(r => r.day == Weekdays.Mon);
            monday.totalSessions.Should().Be(6);
            monday.multiplier.Should().Be(1.5);

            var tuesday = result.Single(r => r.day == Weekdays.Tue);
            tuesday.totalSessions.Should().Be(2);
            tuesday.multiplier.Should().Be(1.0);
        }

        [Fact]
        public void GetWeeklyActivityPatterns_WithZeroAverageClientSessions_ReturnsEmptyList()
        {
            // Arrange - a zero divisor would otherwise produce Infinity multipliers,
            // so the helper short-circuits to an empty list for the UI to handle.
            var records = new List<TrainerDailyRevenue>
            {
                Record(new DateOnly(2024, 1, 1), sessions: 3), // Mon
                Record(new DateOnly(2024, 1, 2), sessions: 4), // Tue
            };

            // Act
            var result = WeekdayActivityPatternHelper.GetWeeklyActivityPatterns(records, averageClientSessions: 0);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, Weekdays.Mon)]
        [InlineData(DayOfWeek.Tuesday, Weekdays.Tue)]
        [InlineData(DayOfWeek.Wednesday, Weekdays.Wed)]
        [InlineData(DayOfWeek.Thursday, Weekdays.Thu)]
        [InlineData(DayOfWeek.Friday, Weekdays.Fri)]
        [InlineData(DayOfWeek.Saturday, Weekdays.Sat)]
        [InlineData(DayOfWeek.Sunday, Weekdays.Sun)]
        public void ReturnWeekdayEnumFromString_MapsEachDayCorrectly(DayOfWeek input, Weekdays expected)
        {
            WeekdayActivityPatternHelper.ReturnWeekdayEnumFromString(input).Should().Be(expected);
        }

        [Fact]
        public void ReturnWeekdayEnumFromString_ThrowsForUnsupportedValue()
        {
            var action = () => WeekdayActivityPatternHelper.ReturnWeekdayEnumFromString((DayOfWeek)99);
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static TrainerDailyRevenue Record(DateOnly date, int sessions) => new()
        {
            AsOfDate = date,
            SessionsToday = sessions,
            AverageSessionPrice = 50m
        };
    }
}
