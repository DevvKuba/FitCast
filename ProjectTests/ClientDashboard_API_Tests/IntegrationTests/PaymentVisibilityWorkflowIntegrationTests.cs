using System.Net.Http.Json;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API_Tests.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientDashboard_API_Tests.IntegrationTests
{
    public class PaymentVisibilityWorkflowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PaymentVisibilityWorkflowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task FilterClientPayments_ShouldHidePaymentsForSoftDeletedClientsOnly()
        {
            await _factory.ResetDatabaseAsync();

            int trainerId;
            int deletedClientPaymentId;
            int activeClientPaymentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer
                {
                    FirstName = "payment-trainer",
                    Role = UserRole.Trainer,
                    DefaultCurrency = "£"
                };

                var deletedClient = new Client
                {
                    FirstName = "old-client",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    IsActive = false,
                    IsDeleted = true,
                    DeletedAt = DateTime.UtcNow.AddDays(-15),
                    CurrentBlockSession = 0
                };

                var activeClient = new Client
                {
                    FirstName = "active-client",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    IsActive = true,
                    CurrentBlockSession = 1,
                    TotalBlockSessions = 10
                };

                var deletedClientPayment = new Payment
                {
                    Trainer = trainer,
                    Client = deletedClient,
                    Amount = 150m,
                    Currency = "£",
                    NumberOfSessions = 6,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                    IsVisible = true
                };

                var activeClientPayment = new Payment
                {
                    Trainer = trainer,
                    Client = activeClient,
                    Amount = 200m,
                    Currency = "£",
                    NumberOfSessions = 8,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsVisible = true
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.AddRange(deletedClient, activeClient);
                dbContext.Payments.AddRange(deletedClientPayment, activeClientPayment);

                await dbContext.SaveChangesAsync();

                trainerId = trainer.Id;
                deletedClientPaymentId = deletedClientPayment.Id;
                activeClientPaymentId = activeClientPayment.Id;
            }

            var trainerHttp = CreateAuthorizedClient(trainerId);

            var response = await trainerHttp.PutAsync($"/api/Payment/filterClientPayments?trainerId={trainerId}", null);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ApiResponseDto<int?>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().Be(1);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var visiblePayments = await dbContext.Payments.ToListAsync();
                visiblePayments.Select(p => p.Id).Should().Contain(activeClientPaymentId);
                visiblePayments.Select(p => p.Id).Should().NotContain(deletedClientPaymentId);

                var allPayments = await dbContext.Payments.IgnoreQueryFilters().ToListAsync();
                var deletedPayment = allPayments.Single(p => p.Id == deletedClientPaymentId);
                var activePayment = allPayments.Single(p => p.Id == activeClientPaymentId);

                deletedPayment.IsVisible.Should().BeFalse();
                activePayment.IsVisible.Should().BeTrue();
            }
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
