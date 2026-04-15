using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Services;
using FluentAssertions;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class TrainerAnalyticsServiceTests
    {
        private readonly TrainerAnalyticsService _service;

        public TrainerAnalyticsServiceTests()
        {
            _service = new TrainerAnalyticsService();
        }

        [Fact]
        public void GetClientMetrics_WithValidData_ReturnsClientMetricsDto()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetClientMetrics(revenueRecords);

            // Assert
            result.Should().NotBeNull();
            result.BaseClients.Should().BeGreaterThan(0);
            result.SessionsPerClient.Should().BeGreaterThanOrEqualTo(0);
            result.AcquiredClients.Should().BeGreaterThanOrEqualTo(0);
            result.ChurnedClients.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void GetClientMetrics_WithSingleMonth_ThrowsInvalidOperationException()
        {
            // Arrange - only 28 days of data (less than one full month)
            var revenueRecords = CreateSampleRevenueRecords(28, startDate: new DateOnly(2024, 1, 1));

            // Act & Assert
            var action = () => _service.GetClientMetrics(revenueRecords);
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*at least one full month*");
        }

        [Fact]
        public void GetClientMetrics_WithThreeMonths_CalculatesAcquisitionAndChurnRates()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetClientMetrics(revenueRecords);

            // Assert
            result.AcquisitionPercentage.Should().BeGreaterThanOrEqualTo(0);
            result.ChurnPercentage.Should().BeGreaterThanOrEqualTo(0);
            result.NetGrowth.Should().Be(result.AcquiredClients - result.ChurnedClients);
            result.NetGrowthPercentage.Should().Be(result.AcquisitionPercentage - result.ChurnPercentage);
        }

        [Fact]
        public void GetClientMetrics_WithGrowingClientBase_NetGrowthIsPositive()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));
            // Modify records to show growth
            for (int i = 0; i < revenueRecords.Count; i++)
            {
                revenueRecords[i].ActiveClients = 10 + (i / 30); // Increases every month
            }

            // Act
            var result = _service.GetClientMetrics(revenueRecords);

            // Assert
            result.NetGrowth.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void GetRevenuePatterns_WithValidData_ReturnsRevenuePatternsDto()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetRevenuePatterns(revenueRecords);

            // Assert
            result.Should().NotBeNull();
            result.MonthlyWorkingDays.Should().BeGreaterThan(0);
            result.SessionsPrice.Should().BeGreaterThan(0);
            result.RevenuePerWorkingDay.Should().BeGreaterThan(0);
            result.RevenuePerWorkingWeek.Should().BeGreaterThan(0);
            result.RevenuePerWorkingMonth.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetRevenuePatterns_WithSingleMonth_ThrowsInvalidOperationException()
        {
            // Arrange - only 28 days of data
            var revenueRecords = CreateSampleRevenueRecords(28, startDate: new DateOnly(2024, 1, 1));

            // Act & Assert
            var action = () => _service.GetRevenuePatterns(revenueRecords);
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetRevenuePatterns_MonthlyRevenueIsGreaterThanWeekly()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetRevenuePatterns(revenueRecords);

            // Assert
            result.RevenuePerWorkingMonth.Should().BeGreaterThan(result.RevenuePerWorkingWeek);
            result.RevenuePerWorkingWeek.Should().BeGreaterThan(result.RevenuePerWorkingDay);
        }

        [Fact]
        public void GetRevenuePatterns_CalculatesSessionPriceCorrectly()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(60, startDate: new DateOnly(2024, 1, 1));
            // Set consistent session price
            foreach (var record in revenueRecords)
            {
                record.AverageSessionPrice = 50m;
            }

            // Act
            var result = _service.GetRevenuePatterns(revenueRecords);

            // Assert
            result.SessionsPrice.Should().Be(50);
        }

        [Fact]
        public void GetActivityPatterns_WithValidData_ReturnsActivityPatternsDto()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetActivityPatterns(revenueRecords);

            // Assert
            result.Should().NotBeNull();
            result.AllWeekdays.Should().NotBeEmpty();
            result.BusiestDays.Should().NotBeEmpty().And.HaveCount(2);
            result.LightDays.Should().NotBeEmpty().And.HaveCount(2);
        }

        [Fact]
        public void GetActivityPatterns_BusiestDaysAreOrderedByActivityDescending()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetActivityPatterns(revenueRecords);

            // Assert
            result.BusiestDays[0].multiplier.Should().BeGreaterThanOrEqualTo(result.BusiestDays[1].multiplier);
        }

        [Fact]
        public void GetActivityPatterns_LightestDaysAreOrderedByActivityAscending()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetActivityPatterns(revenueRecords);

            // Assert
            result.LightDays[0].multiplier.Should().BeLessThanOrEqualTo(result.LightDays[1].multiplier);
        }

        [Fact]
        public void GetAllAnalyticMetrics_WithValidData_ReturnsCompleteAnalyticsDto()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var result = _service.GetAllAnalyticMetrics(revenueRecords);

            // Assert
            result.Should().NotBeNull();
            result.BaseClients.Should().BeGreaterThan(0);
            result.SessionsPrice.Should().BeGreaterThan(0);
            result.MonthlyWorkingDays.Should().BeGreaterThan(0);
            result.AllWeekdays.Should().NotBeEmpty();
        }

        [Fact]
        public void GetAllAnalyticMetrics_AggregatesAllMetricsCorrectly()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(90, startDate: new DateOnly(2024, 1, 1));

            // Act
            var completeMetrics = _service.GetAllAnalyticMetrics(revenueRecords);
            var clientMetrics = _service.GetClientMetrics(revenueRecords);
            var revenuePatterns = _service.GetRevenuePatterns(revenueRecords);
            var activityPatterns = _service.GetActivityPatterns(revenueRecords);

            // Assert
            completeMetrics.BaseClients.Should().Be(clientMetrics.BaseClients);
            completeMetrics.SessionsPrice.Should().Be(revenuePatterns.SessionsPrice);
            completeMetrics.AllWeekdays.Should().HaveCount(activityPatterns.AllWeekdays.Count);
        }

        [Fact]
        public void GetAllAnalyticMetrics_WithSingleMonth_ThrowsInvalidOperationException()
        {
            // Arrange
            var revenueRecords = CreateSampleRevenueRecords(28, startDate: new DateOnly(2024, 1, 1));

            // Act & Assert
            var action = () => _service.GetAllAnalyticMetrics(revenueRecords);
            action.Should().Throw<InvalidOperationException>();
        }

        private List<TrainerDailyRevenue> CreateSampleRevenueRecords(int numberOfDays, DateOnly startDate)
        {
            var records = new List<TrainerDailyRevenue>();
            var currentDate = startDate;
            int activeClients = 10;
            decimal sessionPrice = 40m;
            int totalSessionsThisMonth = 0;
            decimal monthlyRevenueThisFar = 0m;
            int newClientsThisMonth = 0;

            for (int i = 0; i < numberOfDays; i++)
            {
                // Reset monthly counters at month start
                if (currentDate.Day == 1)
                {
                    totalSessionsThisMonth = 0;
                    monthlyRevenueThisFar = 0m;
                    newClientsThisMonth = i > 0 ? 1 : 0;
                }

                // Add variety to daily sessions based on day of week
                double dayMultiplier = currentDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => 1.5,
                    DayOfWeek.Sunday => 0.4,
                    _ => 1.0
                };

                decimal revenueToday = (decimal)(3 * dayMultiplier * (double)sessionPrice);
                int sessionsToday = (int)(3 * dayMultiplier);

                monthlyRevenueThisFar += revenueToday;
                totalSessionsThisMonth += sessionsToday;

                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = 1,
                    AsOfDate = currentDate,
                    RevenueToday = revenueToday,
                    MonthlyRevenueThusFar = monthlyRevenueThisFar,
                    TotalSessionsThisMonth = totalSessionsThisMonth,
                    NewClientsThisMonth = newClientsThisMonth,
                    ActiveClients = activeClients,
                    AverageSessionPrice = sessionPrice
                });

                currentDate = currentDate.AddDays(1);
            }

            return records;
        }

    }
}
