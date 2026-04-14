using System.Net.Http.Json;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Data;
using ClientDashboard_API_Tests.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientDashboard_API_Tests.IntegrationTests
{
    public class CoreWorkflowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CoreWorkflowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task QuickAddWorkout_ShouldCreateWorkout_AndCreateTrainerNotification()
        {
            await _factory.ResetDatabaseAsync();

            int clientId;
            int trainerId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer
                {
                    FirstName = "trainer-one",
                    Role = UserRole.Trainer,
                    NotificationsEnabled = false,
                    DefaultCurrency = "£"
                };

                var seededClient = new Client
                {
                    FirstName = "client-one",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    CurrentBlockSession = 1,
                    TotalBlockSessions = 6,
                    IsActive = true
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.Add(seededClient);
                await dbContext.SaveChangesAsync();

                clientId = seededClient.Id;
                trainerId = trainer.Id;
            }

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Trainer");
            client.DefaultRequestHeaders.Add("X-Test-UserId", trainerId.ToString());

            var quickAddBody = new
            {
                Id = clientId,
                FirstName = "client-one",
                Role = UserRole.Client
            };

            var response = await client.PostAsJsonAsync("/api/Workout/quickAddWorkout", quickAddBody);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<string>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var workouts = await dbContext.Workouts.Where(w => w.ClientId == clientId).ToListAsync();
                workouts.Should().HaveCount(1);
                workouts[0].WorkoutTitle.Should().Contain("Quick Added Workout");

                var updatedClient = await dbContext.Client.FirstAsync(c => c.Id == clientId);
                updatedClient.CurrentBlockSession.Should().Be(2);

                var notifications = await dbContext.Notification
                    .Where(n => n.TrainerId == trainerId && n.Audience == NotificationAudience.Trainer)
                    .ToListAsync();

                notifications.Should().ContainSingle();
                notifications[0].ReminderType.Should().Be(NotificationType.QuickAddWorkoutReminder);

                var statuses = await dbContext.NotificationRecipientStatuses
                    .Where(s => s.UserId == trainerId)
                    .ToListAsync();

                statuses.Should().ContainSingle();
                statuses[0].IsRead.Should().BeFalse();
            }
        }

        [Fact]
        public async Task DeleteClientById_ShouldSoftDeleteClient_AndHideItFromRegularQueries()
        {
            await _factory.ResetDatabaseAsync();

            int clientId;
            int trainerId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer
                {
                    FirstName = "trainer-two",
                    Role = UserRole.Trainer
                };

                var seededClient = new Client
                {
                    FirstName = "client-two",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    IsActive = true,
                    CurrentBlockSession = 0
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.Add(seededClient);
                await dbContext.SaveChangesAsync();

                clientId = seededClient.Id;
                trainerId = trainer.Id;
            }

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Trainer");
            client.DefaultRequestHeaders.Add("X-Test-UserId", trainerId.ToString());

            var response = await client.DeleteAsync($"/api/Client/ById?clientId={clientId}");
            response.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var visibleClient = await dbContext.Client.FirstOrDefaultAsync(c => c.Id == clientId);
                visibleClient.Should().BeNull();

                var deletedClient = await dbContext.Client.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == clientId);
                deletedClient.Should().NotBeNull();
                deletedClient!.IsDeleted.Should().BeTrue();
                deletedClient.DeletedAt.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task DeletePayment_ShouldSetInvisible_AndHidePaymentFromRegularQueries()
        {
            await _factory.ResetDatabaseAsync();

            int paymentId;
            int trainerId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer
                {
                    FirstName = "trainer-three",
                    Role = UserRole.Trainer,
                    DefaultCurrency = "£"
                };

                var client = new Client
                {
                    FirstName = "client-three",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    IsActive = true,
                    CurrentBlockSession = 1,
                    TotalBlockSessions = 8
                };

                var payment = new Payment
                {
                    Trainer = trainer,
                    Client = client,
                    Amount = 200,
                    Currency = "£",
                    NumberOfSessions = 8,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.Add(client);
                dbContext.Payments.Add(payment);
                await dbContext.SaveChangesAsync();

                paymentId = payment.Id;
                trainerId = trainer.Id;
            }

            var clientHttp = _factory.CreateClient();
            clientHttp.DefaultRequestHeaders.Add("X-Test-Role", "Trainer");
            clientHttp.DefaultRequestHeaders.Add("X-Test-UserId", trainerId.ToString());

            var response = await clientHttp.PutAsync($"/api/Payment/deletePayment?paymentId={paymentId}", null);
            response.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var visiblePayment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
                visiblePayment.Should().BeNull();

                var deletedPayment = await dbContext.Payments.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == paymentId);
                deletedPayment.Should().NotBeNull();
                deletedPayment!.IsVisible.Should().BeFalse();
            }
        }
    }
}
