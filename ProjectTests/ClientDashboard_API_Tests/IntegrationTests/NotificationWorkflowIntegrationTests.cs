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
    public class NotificationWorkflowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationWorkflowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ClientReminderFlow_ShouldCreateNotification_AndSupportReadLifecycle()
        {
            await _factory.ResetDatabaseAsync();

            int trainerId;
            int clientId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer
                {
                    FirstName = "notify-trainer",
                    Role = UserRole.Trainer,
                    NotificationsEnabled = false
                };

                var client = new Client
                {
                    FirstName = "notify-client",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    CurrentBlockSession = 4,
                    TotalBlockSessions = 4,
                    IsActive = true,
                    NotificationsEnabled = false
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.Add(client);
                await dbContext.SaveChangesAsync();

                trainerId = trainer.Id;
                clientId = client.Id;
            }

            var trainerHttp = CreateAuthorizedClient("Trainer", trainerId);

            var sendResponse = await trainerHttp.PostAsync($"/api/Notification/SendClientBlockCompletionReminder?trainerId={trainerId}&clientId={clientId}", null);
            sendResponse.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var clientNotification = await dbContext.Notification
                    .OrderByDescending(n => n.SentAt)
                    .FirstOrDefaultAsync();

                clientNotification.Should().NotBeNull();
                clientNotification!.Audience.Should().Be(NotificationAudience.Client);
                clientNotification.ReminderType.Should().Be(NotificationType.ClientBlockCompletionReminder);
            }

            var clientHttp = CreateAuthorizedClient("Client", clientId);

            var unreadBefore = await clientHttp.GetFromJsonAsync<ApiResponseDto<int?>>($"/api/Notification/gatherUnreadUserNotificationCount?userId={clientId}");
            unreadBefore.Should().NotBeNull();
            unreadBefore!.Success.Should().BeTrue();
            unreadBefore.Data.Should().Be(1);

            int notificationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                notificationId = await dbContext.Notification.Select(n => n.Id).SingleAsync();
            }

            var markReadBody = new NotificationReadStatusDto
            {
                UserId = clientId,
                NotificationIds = [notificationId]
            };

            var markReadResponse = await clientHttp.PutAsJsonAsync("/api/Notification/markNotificationsAsRead", markReadBody);
            markReadResponse.EnsureSuccessStatusCode();

            var unreadAfter = await clientHttp.GetFromJsonAsync<ApiResponseDto<int?>>($"/api/Notification/gatherUnreadUserNotificationCount?userId={clientId}");
            unreadAfter.Should().NotBeNull();
            unreadAfter!.Data.Should().Be(0);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var clientStatus = await dbContext.NotificationRecipientStatuses
                    .SingleAsync(s => s.UserId == clientId && s.NotificationId == notificationId);

                clientStatus.IsRead.Should().BeTrue();
                clientStatus.ReadAt.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GatherLatestNotifications_ShouldReturnAudienceScopedResultsPerUser()
        {
            await _factory.ResetDatabaseAsync();

            int trainerId;
            int clientId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var trainer = new Trainer { FirstName = "aud-trainer", Role = UserRole.Trainer };
                var client = new Client
                {
                    FirstName = "aud-client",
                    Role = UserRole.Client,
                    Trainer = trainer,
                    CurrentBlockSession = 2,
                    TotalBlockSessions = 4,
                    IsActive = true
                };

                dbContext.Trainer.Add(trainer);
                dbContext.Client.Add(client);
                await dbContext.SaveChangesAsync();

                trainerId = trainer.Id;
                clientId = client.Id;
            }

            var trainerHttp = CreateAuthorizedClient("Trainer", trainerId);
            (await trainerHttp.PostAsync($"/api/Notification/SendTrainerBlockCompletionReminder?trainerId={trainerId}&clientId={clientId}", null)).EnsureSuccessStatusCode();
            (await trainerHttp.PostAsync($"/api/Notification/SendClientBlockCompletionReminder?trainerId={trainerId}&clientId={clientId}", null)).EnsureSuccessStatusCode();

            var trainerLatest = await trainerHttp.GetFromJsonAsync<ApiResponseDto<List<Notification>>>($"/api/Notification/gatherLatestUserNotifications?userId={trainerId}");
            trainerLatest.Should().NotBeNull();
            trainerLatest!.Success.Should().BeTrue();
            trainerLatest.Data.Should().NotBeNull();
            trainerLatest.Data!.Should().OnlyContain(n => n.Audience == NotificationAudience.Trainer);

            var clientHttp = CreateAuthorizedClient("Client", clientId);
            var clientLatest = await clientHttp.GetFromJsonAsync<ApiResponseDto<List<Notification>>>($"/api/Notification/gatherLatestUserNotifications?userId={clientId}");
            clientLatest.Should().NotBeNull();
            clientLatest!.Success.Should().BeTrue();
            clientLatest.Data.Should().NotBeNull();
            clientLatest.Data!.Should().OnlyContain(n => n.Audience == NotificationAudience.Client);
        }

        private HttpClient CreateAuthorizedClient(string role, int userId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
            return client;
        }
    }
}
