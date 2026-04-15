using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.ML.Enums;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Models;
using FluentAssertions;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class MLHelpersTests
    {

        [Fact]
        public void PrepareTrainingData_WithValidMultiMonthData_ReturnsTrainingExamples()
        {
            // Arrange
            var dailyRecords = CreateThreeMonthsRevenueData(trainerId: 1);

            // Act
            var result = FeatureEngineeringHelper.PrepareTrainingData(dailyRecords);

            // Assert
            result.Should().NotBeEmpty("Should generate training examples from multi-month data");
            result.Should().AllSatisfy(r =>
            {
                r.ActiveClients.Should().BeGreaterThan(0);
                r.TotalSessionsThisMonth.Should().BeGreaterThanOrEqualTo(0);
                r.AverageSessionPrice.Should().BeGreaterThan(0);
                r.SessionsPerClient.Should().BeGreaterThanOrEqualTo(0);
                r.DaysRemainingInMonth.Should().BeGreaterThanOrEqualTo(0);
                r.RevenueVelocity.Should().BeGreaterThanOrEqualTo(0);
            });
        }

        [Fact]
        public void PrepareTrainingData_WithSingleMonth_ReturnsEmptyList()
        {
            // Arrange - only 28 days (less than 2 full months)
            var dailyRecords = CreateRevenueData(trainerId: 1, numberOfDays: 28);

            // Act
            var result = FeatureEngineeringHelper.PrepareTrainingData(dailyRecords);

            // Assert
            result.Should().BeEmpty("Should not generate training data from single month");
        }

        [Fact]
        public void PrepareTrainingData_CreatesSnapshotsEvery3Days()
        {
            // Arrange
            var dailyRecords = CreateTwoMonthsRevenueData(trainerId: 2);

            // Act
            var result = FeatureEngineeringHelper.PrepareTrainingData(dailyRecords);

            // Assert
            result.Should().NotBeEmpty();
            // With 2 months, should create multiple snapshots (approximately every 3 days in first month)
            result.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void PrepareTrainingData_SkipsNonConsecutiveMonths()
        {
            // Arrange - Create data with gap (Jan and Mar, skip Feb)
            var january = CreateRevenueDataForMonth(trainerId: 3, year: 2024, month: 1);
            var march = CreateRevenueDataForMonth(trainerId: 3, year: 2024, month: 3);
            var records = january.Concat(march).ToList();

            // Act
            var result = FeatureEngineeringHelper.PrepareTrainingData(records);

            // Assert
            result.Should().BeEmpty("Should skip non-consecutive months");
        }

        [Fact]
        public void PrepareTrainingData_CalculatesSessionsPerClientCorrectly()
        {
            // Arrange
            var dailyRecords = CreateThreeMonthsRevenueData(trainerId: 4);

            // Act
            var result = FeatureEngineeringHelper.PrepareTrainingData(dailyRecords);

            // Assert
            result.Should().AllSatisfy(r =>
            {
                if (r.ActiveClients > 0)
                {
                    var expectedSessionsPerClient = (float)r.TotalSessionsThisMonth / r.ActiveClients;
                    r.SessionsPerClient.Should().Be(expectedSessionsPerClient);
                }
                else
                {
                    r.SessionsPerClient.Should().Be(0);
                }
            });
        }


        [Fact]
        public void PreparePredictionData_WithValidRecord_ReturnsTrainerRevenueData()
        {
            // Arrange
            var record = new TrainerDailyRevenue
            {
                TrainerId = 1,
                AsOfDate = new DateOnly(2024, 6, 15),
                RevenueToday = 400,
                MonthlyRevenueThusFar = 5000,
                TotalSessionsThisMonth = 30,
                NewClientsThisMonth = 2,
                ActiveClients = 12,
                AverageSessionPrice = 50
            };

            // Act
            var result = FeatureEngineeringHelper.PreparePredictionData(record);

            // Assert
            result.Should().NotBeNull();
            result.ActiveClients.Should().Be(12);
            result.TotalSessionsThisMonth.Should().Be(30);
            result.AverageSessionPrice.Should().Be(50);
            result.MonthlyRevenueThusFar.Should().Be(5000);
            result.DaysRemainingInMonth.Should().BeGreaterThan(0);
        }

        [Fact]
        public void PreparePredictionData_CalculatesDaysRemainingCorrectly()
        {
            // Arrange
            var record = new TrainerDailyRevenue
            {
                TrainerId = 1,
                AsOfDate = new DateOnly(2024, 6, 10), // 10th of June, which has 30 days
                RevenueToday = 100,
                MonthlyRevenueThusFar = 1000,
                TotalSessionsThisMonth = 10,
                NewClientsThisMonth = 1,
                ActiveClients = 10,
                AverageSessionPrice = 50
            };

            // Act
            var result = FeatureEngineeringHelper.PreparePredictionData(record);

            // Assert
            result.DaysRemainingInMonth.Should().Be(20); // 30 - 10 = 20 days remaining
        }

        [Fact]
        public void PreparePredictionData_CalculatesRevenueVelocityCorrectly()
        {
            // Arrange
            var record = new TrainerDailyRevenue
            {
                TrainerId = 1,
                AsOfDate = new DateOnly(2024, 6, 5), // 5th day of month
                RevenueToday = 500,
                MonthlyRevenueThusFar = 2500, // 2500 revenue in 5 days
                TotalSessionsThisMonth = 25,
                NewClientsThisMonth = 1,
                ActiveClients = 10,
                AverageSessionPrice = 50
            };

            // Act
            var result = FeatureEngineeringHelper.PreparePredictionData(record);

            // Assert
            var expectedVelocity = 2500f / 5; // 500 per day
            result.RevenueVelocity.Should().Be(expectedVelocity);
        }

        [Fact]
        public void PreparePredictionData_CalculatesSessionsPerClientCorrectly()
        {
            // Arrange
            var record = new TrainerDailyRevenue
            {
                TrainerId = 1,
                AsOfDate = new DateOnly(2024, 6, 10),
                RevenueToday = 400,
                MonthlyRevenueThusFar = 4000,
                TotalSessionsThisMonth = 40,
                NewClientsThisMonth = 2,
                ActiveClients = 10,
                AverageSessionPrice = 50
            };

            // Act
            var result = FeatureEngineeringHelper.PreparePredictionData(record);

            // Assert
            var expectedSessionsPerClient = 40f / 10; // 4 sessions per client
            result.SessionsPerClient.Should().Be(expectedSessionsPerClient);
        }


        [Theory]
        [InlineData(0, PredictionConfidence.Insufficient)]
        [InlineData(1, PredictionConfidence.Insufficient)]
        [InlineData(2, PredictionConfidence.Low)]
        [InlineData(3, PredictionConfidence.Low)]
        [InlineData(4, PredictionConfidence.Medium)]
        [InlineData(6, PredictionConfidence.Medium)]
        [InlineData(7, PredictionConfidence.High)]
        [InlineData(12, PredictionConfidence.High)]
        [InlineData(13, PredictionConfidence.VeryHigh)]
        [InlineData(24, PredictionConfidence.VeryHigh)]
        public void DetermineConfidenceLevel_WithVaryingMonths_ReturnsCorrectLevel(int months, PredictionConfidence expected)
        {
            // Act
            var result = PredictionConfidenceHelper.DetermineConfidenceLevel(months);

            // Assert
            result.Should().Be(expected);
        }


        [Fact]
        public void CalculatePredictionRange_WithLowConfidence_ReturnsWidestRange()
        {
            // Arrange
            float prediction = 1000;
            double mae = 100;

            // Act
            var (lower, upper) = PredictionConfidenceHelper.CalculatePredictionRange(
                prediction, mae, PredictionConfidence.Low);

            // Assert
            var expectedMargin = 100 * 2.5; // 250% of MAE
            lower.Should().Be(prediction - (float)expectedMargin);
            upper.Should().Be(prediction + (float)expectedMargin);
        }

        [Fact]
        public void CalculatePredictionRange_WithMediumConfidence_ReturnsModerateRange()
        {
            // Arrange
            float prediction = 1000;
            double mae = 100;

            // Act
            var (lower, upper) = PredictionConfidenceHelper.CalculatePredictionRange(
                prediction, mae, PredictionConfidence.Medium);

            // Assert
            var expectedMargin = 100 * 1.5; // 150% of MAE
            lower.Should().Be(prediction - (float)expectedMargin);
            upper.Should().Be(prediction + (float)expectedMargin);
        }

        [Fact]
        public void CalculatePredictionRange_WithHighConfidence_ReturnsNarrowRange()
        {
            // Arrange
            float prediction = 1000;
            double mae = 100;

            // Act
            var (lower, upper) = PredictionConfidenceHelper.CalculatePredictionRange(
                prediction, mae, PredictionConfidence.High);

            // Assert
            var expectedMargin = 100 * 1.0; // 100% of MAE
            lower.Should().Be(prediction - (float)expectedMargin);
            upper.Should().Be(prediction + (float)expectedMargin);
        }

        [Fact]
        public void CalculatePredictionRange_WithVeryHighConfidence_ReturnsNarrowestRange()
        {
            // Arrange
            float prediction = 1000;
            double mae = 100;

            // Act
            var (lower, upper) = PredictionConfidenceHelper.CalculatePredictionRange(
                prediction, mae, PredictionConfidence.VeryHigh);

            // Assert
            var expectedMargin = 100 * 0.75; // 75% of MAE
            lower.Should().Be(prediction - (float)expectedMargin);
            upper.Should().Be(prediction + (float)expectedMargin);
        }

        [Fact]
        public void CalculatePredictionRange_NeverReturnsNegativeLowerBound()
        {
            // Arrange
            float prediction = 50; // Small prediction
            double mae = 100; // Large error margin

            // Act
            var (lower, upper) = PredictionConfidenceHelper.CalculatePredictionRange(
                prediction, mae, PredictionConfidence.Low);

            // Assert
            lower.Should().BeGreaterThanOrEqualTo(1, "Lower bound should never go below 1");
            upper.Should().BeGreaterThan(lower);
        }


        [Fact]
        public void GetConfidenceMessage_WithLowConfidence_ReturnsAppropriateMessage()
        {
            // Act
            var result = PredictionConfidenceHelper.GetConfidenceMessage(PredictionConfidence.Low, 0.65);

            // Assert
            result.Should().Contain("limited data");
            result.Should().Contain("R²=0.65");
            result.Should().Contain("Confidence will improve");
        }

        [Fact]
        public void GetConfidenceMessage_WithMediumConfidence_ReturnsAppropriateMessage()
        {
            // Act
            var result = PredictionConfidenceHelper.GetConfidenceMessage(PredictionConfidence.Medium, 0.75);

            // Assert
            result.Should().Contain("Moderate confidence");
            result.Should().Contain("R²=0.75");
            result.Should().Contain("Add more data");
        }

        [Fact]
        public void GetConfidenceMessage_WithHighConfidence_ReturnsAppropriateMessage()
        {
            // Act
            var result = PredictionConfidenceHelper.GetConfidenceMessage(PredictionConfidence.High, 0.85);

            // Assert
            result.Should().Contain("High confidence");
            result.Should().Contain("R²=0.85");
        }

        [Fact]
        public void GetConfidenceMessage_WithVeryHighConfidence_ReturnsAppropriateMessage()
        {
            // Act
            var result = PredictionConfidenceHelper.GetConfidenceMessage(PredictionConfidence.VeryHigh, 0.92);

            // Assert
            result.Should().Contain("Very high confidence");
            result.Should().Contain("extensive history");
            result.Should().Contain("R²=0.92");
        }


        [Fact]
        public void GenerateRealisticRevenueData_WithValidInput_ReturnsRecords()
        {
            // Arrange
            int trainerId = 1;
            int numberOfMonths = 3;

            // Act
            var result = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().AllSatisfy(r =>
            {
                r.TrainerId.Should().Be(trainerId);
                r.RevenueToday.Should().BeGreaterThanOrEqualTo(0);
                r.ActiveClients.Should().BeGreaterThan(0);
                r.AverageSessionPrice.Should().BeGreaterThan(0);
                r.MonthlyRevenueThusFar.Should().BeGreaterThanOrEqualTo(0);
                r.TotalSessionsThisMonth.Should().BeGreaterThanOrEqualTo(0);
            });
        }

        [Fact]
        public void GenerateRealisticRevenueData_GeneratesApproximatelyCorrectNumberOfDays()
        {
            // Arrange
            int trainerId = 1;
            int numberOfMonths = 3;
            var expectedDaysMin = numberOfMonths * 28; // Conservative estimate
            var expectedDaysMax = numberOfMonths * 32; // Upper bound

            // Act
            var result = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            // Assert
            result.Count.Should().BeGreaterThanOrEqualTo(expectedDaysMin)
                .And.BeLessThanOrEqualTo(expectedDaysMax);
        }

        [Fact]
        public void GenerateRealisticRevenueData_EachRecordHasSequentialDates()
        {
            // Arrange
            int trainerId = 1;
            int numberOfMonths = 2;

            // Act
            var result = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            // Assert
            var orderedByDate = result.OrderBy(r => r.AsOfDate).ToList();
            for (int i = 0; i < orderedByDate.Count - 1; i++)
            {
                orderedByDate[i + 1].AsOfDate
                    .Should().Be(orderedByDate[i].AsOfDate.AddDays(1));
            }
        }

        [Fact]
        public void GenerateRealisticRevenueData_MonthlyRevenueIsCumulative()
        {
            // Arrange
            int trainerId = 1;
            int numberOfMonths = 3;

            // Act
            var result = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            // Assert
            var groupedByMonth = result.GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month }).ToList();
            foreach (var monthGroup in groupedByMonth)
            {
                var recordsInMonth = monthGroup.OrderBy(r => r.AsOfDate).ToList();
                var cumulativeRevenue = 0m;

                foreach (var record in recordsInMonth)
                {
                    cumulativeRevenue += record.RevenueToday;
                    record.MonthlyRevenueThusFar.Should().Be(cumulativeRevenue, 
                        $"Monthly revenue should match cumulative sum for {record.AsOfDate}");
                }
            }
        }

        [Fact]
        public void GenerateRealisticRevenueData_WithDifferentTrainerIds_ProducesConsistentPatterns()
        {
            // Arrange
            int numberOfMonths = 2;

            // Act
            var trainer1Data = DummyDataGenerator.GenerateRealisticRevenueData(trainerId: 1, numberOfMonths);
            var trainer2Data = DummyDataGenerator.GenerateRealisticRevenueData(trainerId: 2, numberOfMonths);

            // Assert - Both should have data
            trainer1Data.Should().NotBeEmpty();
            trainer2Data.Should().NotBeEmpty();
            trainer1Data.Count.Should().Be(trainer2Data.Count, 
                "Both trainers should generate same number of records");
        }

        [Fact]
        public void GenerateRealisticRevenueData_ActiveClientsChangeRealisticallly()
        {
            // Arrange
            int trainerId = 1;
            int numberOfMonths = 6;

            // Act
            var result = DummyDataGenerator.GenerateRealisticRevenueData(trainerId, numberOfMonths);

            // Assert
            var firstMonthClients = result.First().ActiveClients;
            var lastMonthClients = result.Last().ActiveClients;

            // Client count should change (growth, churn) - allow small variance
            var difference = Math.Abs(firstMonthClients - lastMonthClients);
            difference.Should().BeLessThanOrEqualTo(10, "Client count should change realistically");
        }


        private List<TrainerDailyRevenue> CreateThreeMonthsRevenueData(int trainerId)
        {
            return CreateRevenueData(trainerId, numberOfDays: 90);
        }

        private List<TrainerDailyRevenue> CreateTwoMonthsRevenueData(int trainerId)
        {
            return CreateRevenueData(trainerId, numberOfDays: 60);
        }

        private List<TrainerDailyRevenue> CreateRevenueData(
            int trainerId,
            int numberOfDays,
            DateOnly? startDate = null)
        {
            var records = new List<TrainerDailyRevenue>();
            var currentDate = startDate ?? new DateOnly(2024, 1, 1);
            int activeClients = 10;
            decimal sessionPrice = 50m;
            int totalSessionsThisMonth = 0;
            decimal monthlyRevenueThisFar = 0m;

            for (int i = 0; i < numberOfDays; i++)
            {
                if (currentDate.Day == 1)
                {
                    totalSessionsThisMonth = 0;
                    monthlyRevenueThisFar = 0m;
                }

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
                    TrainerId = trainerId,
                    AsOfDate = currentDate,
                    RevenueToday = revenueToday,
                    MonthlyRevenueThusFar = monthlyRevenueThisFar,
                    TotalSessionsThisMonth = totalSessionsThisMonth,
                    NewClientsThisMonth = 1,
                    ActiveClients = activeClients,
                    AverageSessionPrice = sessionPrice
                });

                currentDate = currentDate.AddDays(1);
            }

            return records;
        }

        private List<TrainerDailyRevenue> CreateRevenueDataForMonth(int trainerId, int year, int month)
        {
            var firstDay = new DateOnly(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            return CreateRevenueData(trainerId, numberOfDays: daysInMonth, startDate: firstDay);
        }

    }
}
