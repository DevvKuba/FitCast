using System.Net.Http.Json;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API_Tests.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClientDashboard_API_Tests.IntegrationTests
{
    public class TrainerAnalyticsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public TrainerAnalyticsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetTrainerLastMonthsAnalytics_ShouldReturnAggregatedAnalytics()
        {
            await _factory.ResetDatabaseAsync();

            var trainerId = await SeedTrainerWithRevenueHistoryAsync();
            var trainerHttp = CreateAuthorizedClient(trainerId);

            var response = await trainerHttp.GetFromJsonAsync<ApiResponseDto<CompleteTrainerAnalyticsDto>>($"/api/Trainer/getTrainerLastMonthsAnalytics?trainerId={trainerId}");

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.RevenuePerWorkingMonth.Should().BeGreaterThan(0);
            response.Data.MonthlyWorkingDays.Should().BeGreaterThan(0);
            response.Data.AllWeekdays.Should().HaveCount(7);
        }

        [Fact]
        public async Task GetTrainerAllMonthsAnalytics_ShouldReturnHistoryWideAnalytics()
        {
            await _factory.ResetDatabaseAsync();

            var trainerId = await SeedTrainerWithRevenueHistoryAsync();
            var trainerHttp = CreateAuthorizedClient(trainerId);

            var response = await trainerHttp.GetFromJsonAsync<ApiResponseDto<CompleteTrainerAnalyticsDto>>($"/api/Trainer/getTrainerAllMonthsAnalytics?trainerId={trainerId}");

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.BaseClients.Should().BeGreaterThan(0);
            response.Data.SessionsPrice.Should().BeGreaterThan(0);
            response.Data.BusiestDays.Should().NotBeEmpty();
            response.Data.LightDays.Should().NotBeEmpty();
        }

        private async Task<int> SeedTrainerWithRevenueHistoryAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var trainer = new Trainer
            {
                FirstName = "analytics-trainer",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50m
            };

            dbContext.Trainer.Add(trainer);
            await dbContext.SaveChangesAsync();

            var records = new List<TrainerDailyRevenue>();
            records.AddRange(BuildMonthRecords(trainer.Id, 2026, 2, baseActiveClients: 10, dailySessionIncrement: 2, sessionPrice: 50m));
            records.AddRange(BuildMonthRecords(trainer.Id, 2026, 3, baseActiveClients: 11, dailySessionIncrement: 2, sessionPrice: 52m));

            dbContext.TrainerDailyRevenue.AddRange(records);
            await dbContext.SaveChangesAsync();

            return trainer.Id;
        }

        private static List<TrainerDailyRevenue> BuildMonthRecords(int trainerId, int year, int month, int baseActiveClients, int dailySessionIncrement, decimal sessionPrice)
        {
            var list = new List<TrainerDailyRevenue>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var cumulativeRevenue = 0m;
            var cumulativeSessions = 0;

            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(year, month, day);
                var isSunday = date.DayOfWeek == DayOfWeek.Sunday;

                var sessionsToday = isSunday ? 0 : dailySessionIncrement;
                var revenueToday = sessionsToday * sessionPrice;

                cumulativeRevenue += revenueToday;
                cumulativeSessions += sessionsToday;

                list.Add(new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    RevenueToday = revenueToday,
                    MonthlyRevenueThusFar = cumulativeRevenue,
                    TotalSessionsThisMonth = cumulativeSessions,
                    NewClientsThisMonth = month == 2 ? 1 : 2,
                    ActiveClients = baseActiveClients,
                    AverageSessionPrice = sessionPrice,
                    AsOfDate = date
                });
            }

            return list;
        }

        private HttpClient CreateAuthorizedClient(int trainerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Trainer");
            client.DefaultRequestHeaders.Add("X-Test-UserId", trainerId.ToString());
            return client;
        }
    }
}
