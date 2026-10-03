using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class NotificationControllerTests
    {
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly TestTwillioMessageService _messageService;
        private readonly NotificationService _notificationService;
        private readonly NotificationController _notificationController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public class TestTwillioMessageService : IMessageService
        {
            // implement test simulations
            public void InitialiseBaseTwillioClient()
            {
                Console.WriteLine("Initialised client");
            }

            public void PipelineClientBlockCompletionReminder(string clientName)
            {
                Console.WriteLine($"Reminder sent to client: {clientName}");
            }

            public void SendSMSMessage(Trainer? trainer, Client? client, string senderPhoneNumber, string notificationMessage)
            {
                string trainerName = trainer == null ? "unidentified" : trainer.FirstName;
                string clientName = client == null ? "unidentified" : client.FirstName;
                Console.WriteLine($"SMS message sent to trainer: {trainerName} about client: {clientName} with message: {notificationMessage} ");
            }
        }

        public NotificationControllerTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;


            _messageService = new TestTwillioMessageService();
            _notificationService = new NotificationService(_unitOfWork, _messageService);

            var (authorizationService, currentUserAccessor, httpContextAccessor) =
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _notificationController = new NotificationController(_unitOfWork, _notificationService, authorizationService, currentUserAccessor);
            TestAuthHelpers.AttachHttpContext(_notificationController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);
        private void AuthenticateAsClient(int clientId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Client", clientId);

        [Fact]
        public async Task TestSuccessfullySendingTrainerBlockCompletionReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123",
                NotificationsEnabled = true,

            };

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            client.TrainerId = trainer.Id;
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(client.Id);
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(trainer.FirstName, response.Data);
            Assert.Contains("successful", response.Message);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(trainer.Id, notification.TrainerId);
            Assert.Equal(client.Id, notification.ClientId);
            Assert.Equal(NotificationType.TrainerBlockCompletionReminder, notification.ReminderType);
            Assert.Equal(CommunicationType.Sms, notification.SentThrough);
        }

        [Fact]
        public async Task TestTrainerBlockCompletionReminderReturnsNotFoundForInvalidClientIdAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var invalidClientId = 999;

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(invalidClientId);
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestTrainerBlockCompletionReminderReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            var otherTrainer = new Trainer { Role = UserRole.Trainer, FirstName = "Jane", Surname = "Smith", Email = "jane@example.com", PhoneNumber = "+1234567891", PasswordHash = "hash456" };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                TrainerId = owningTrainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(client.Id);
            var forbiddenResult = actionResult.Result as ObjectResult;
            var response = forbiddenResult?.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult!.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.False(await _context.Notification.AnyAsync());
        }

        [Fact]
        public async Task TestSuccessfullySendingClientBlockCompletionReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8,
                NotificationsEnabled = true
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            client.TrainerId = trainer.Id;
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(client.Id);
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Contains("successful", response.Message);

            // Verify notification was saved
            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(trainer.Id, notification.TrainerId);
            Assert.Equal(client.Id, notification.ClientId);
            Assert.Equal(NotificationType.ClientBlockCompletionReminder, notification.ReminderType);
            Assert.Equal(CommunicationType.Sms, notification.SentThrough);
        }

        [Fact]
        public async Task TestClientBlockCompletionReminderReturnsNotFoundForInvalidClientIdAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var invalidClientId = 999;

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(invalidClientId);
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestClientBlockCompletionReminderReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            var otherTrainer = new Trainer { Role = UserRole.Trainer, FirstName = "Jane", Surname = "Smith", Email = "jane@example.com", PhoneNumber = "+1234567891", PasswordHash = "hash456" };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                TrainerId = owningTrainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(client.Id);
            var forbiddenResult = actionResult.Result as ObjectResult;
            var response = forbiddenResult?.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult!.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.False(await _context.Notification.AnyAsync());
        }

        [Fact]
        public async Task TestNotificationIsStoredInDatabaseAfterTrainerReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            client.TrainerId = trainer.Id;
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            await _notificationController.TrainerBlockCompletionReminderAsync(client.Id);

            var notificationCount = await _context.Notification.CountAsync();
            Assert.Equal(1, notificationCount);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains(client.FirstName, notification.Message);
            Assert.Contains("monthly sessions have come to an end", notification.Message);
        }

        [Fact]
        public async Task TestNotificationIsStoredInDatabaseAfterClientReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = UserRole.Trainer,
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = UserRole.Client,
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            client.TrainerId = trainer.Id;
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            await _notificationController.ClientBlockCompletionReminderAsync(client.Id);

            var notificationCount = await _context.Notification.CountAsync();
            Assert.Equal(1, notificationCount);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains(client.FirstName, notification.Message);
            Assert.Contains("monthly sessions", notification.Message);
        }

        [Fact]
        public async Task TestGetUserNotificationStatusAsync_ReturnsCurrentUsersStatus()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123", NotificationsEnabled = true };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.GetUserNotificationStatusAsync();
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task TestGetUserNotificationStatusAsync_ReturnsNotFoundForUnknownUser()
        {
            AuthenticateAsTrainer(999);
            var actionResult = await _notificationController.GetUserNotificationStatusAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;

            Assert.NotNull(notFoundResult);
        }

        [Fact]
        public async Task TestChangeUserNotificationStatusAsync_TogglesStatusForCurrentUserOnly()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123", NotificationsEnabled = false };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.ChangeUserNotificationStatusAsync(new NotificationSmsStatusDto { NotificationStatus = true });
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Contains("enabled", response.Message);

            var updatedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.True(updatedTrainer!.NotificationsEnabled);
        }

        [Fact]
        public async Task TestChangeUserNotificationStatusAsync_ReturnsNotFoundForUnknownUser()
        {
            AuthenticateAsTrainer(999);
            var actionResult = await _notificationController.ChangeUserNotificationStatusAsync(new NotificationSmsStatusDto { NotificationStatus = true });
            var notFoundResult = actionResult.Result as NotFoundObjectResult;

            Assert.NotNull(notFoundResult);
        }

        [Fact]
        public async Task TestChangeNotificationStatusesToReadAsync_MarksCurrentUsersNotificationsAsRead()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification { TrainerId = trainer.Id, Message = "Notification", ReminderType = NotificationType.NewClientConfigurationReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow };
            notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var payload = new NotificationReadStatusDto { UserId = trainer.Id, NotificationIds = [notification.Id] };
            var actionResult = await _notificationController.ChangeNotificationStatusesToReadAsync(payload);
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var status = await _context.NotificationRecipientStatuses.SingleAsync(s => s.UserId == trainer.Id);
            Assert.True(status.IsRead);
        }

        [Fact]
        public async Task TestChangeNotificationStatusesToReadAsync_CannotMarkAnotherUsersNotificationsAsRead()
        {
            // Regression test: the endpoint used to trust the request body's UserId when calling
            // MarkNotificationsAsReadAsync even after the existence-check switched to the JWT-derived id,
            // so a caller could flip another user's recipient-status rows to read.
            var trainerA = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "a@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            var trainerB = new Trainer { Role = UserRole.Trainer, FirstName = "Jane", Surname = "Smith", Email = "b@example.com", PhoneNumber = "+1234567891", PasswordHash = "hash456" };
            await _context.Trainer.AddRangeAsync(trainerA, trainerB);
            await _unitOfWork.Complete();

            var notification = new Notification { TrainerId = trainerA.Id, Message = "Trainer A's notification", ReminderType = NotificationType.NewClientConfigurationReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow };
            notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainerA.Id, IsRead = false });
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            // Authenticated as trainer B, but the body claims trainer A's id -- the JWT identity must win.
            AuthenticateAsTrainer(trainerB.Id);
            var payload = new NotificationReadStatusDto { UserId = trainerA.Id, NotificationIds = [notification.Id] };
            var actionResult = await _notificationController.ChangeNotificationStatusesToReadAsync(payload);
            var badRequestResult = actionResult.Result as ObjectResult;

            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult!.StatusCode);

            var status = await _context.NotificationRecipientStatuses.SingleAsync(s => s.UserId == trainerA.Id);
            Assert.False(status.IsRead);
        }

        [Fact]
        public async Task TestGatherLatestUserNotificationsAsync_ReturnsCurrentUsersNotifications()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification { TrainerId = trainer.Id, Message = "Notification", ReminderType = NotificationType.NewClientConfigurationReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow };
            notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.GatherLatestUserNotificationsAsync();
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<List<NotificationResponseDto>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Single(response.Data!);
            Assert.Equal("Notification", response.Data!.Single().Message);
        }

        [Fact]
        public async Task TestGatherLatestUserNotificationsAsync_ReturnsNotFoundForUnknownUser()
        {
            AuthenticateAsTrainer(999);
            var actionResult = await _notificationController.GatherLatestUserNotificationsAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;

            Assert.NotNull(notFoundResult);
        }

        [Fact]
        public async Task TestGatherAllUserNotificationsAsync_ReturnsAllOfCurrentClientsNotifications()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            var client = new Client { Role = UserRole.Client, FirstName = "Jane", Surname = "Smith", PhoneNumber = "+0987654321", CurrentBlockSession = 8, TotalBlockSessions = 8 };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            for (int i = 0; i < 11; i++)
            {
                var notification = new Notification { TrainerId = trainer.Id, ClientId = client.Id, Message = $"Notification {i}", ReminderType = NotificationType.ClientStepsTrackedNotification, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Client, SentAt = DateTime.UtcNow.AddMinutes(-i) };
                notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = client.Id, IsRead = false });
                await _context.Notification.AddAsync(notification);
            }
            await _unitOfWork.Complete();

            AuthenticateAsClient(client.Id);
            var actionResult = await _notificationController.GatherAllUserNotificationsAsync();
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<List<NotificationResponseDto>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(11, response.Data!.Count);
        }

        [Fact]
        public async Task TestGatherAllUserNotificationsAsync_ReturnsNotFoundForUnknownUser()
        {
            AuthenticateAsTrainer(999);
            var actionResult = await _notificationController.GatherAllUserNotificationsAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;

            Assert.NotNull(notFoundResult);
        }

        [Fact]
        public async Task TestGatherUnreadUserNotificationCountAsync_ReturnsCountForCurrentUser()
        {
            var trainer = new Trainer { Role = UserRole.Trainer, FirstName = "John", Surname = "Doe", Email = "john@example.com", PhoneNumber = "+1234567890", PasswordHash = "hash123" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var readNotification = new Notification { TrainerId = trainer.Id, Message = "Read", ReminderType = NotificationType.NewClientConfigurationReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow };
            readNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = true });

            var unreadNotification = new Notification { TrainerId = trainer.Id, Message = "Unread", ReminderType = NotificationType.NewClientConfigurationReminder, SentThrough = CommunicationType.InApp, Audience = NotificationAudience.Trainer, SentAt = DateTime.UtcNow };
            unreadNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });

            await _context.Notification.AddRangeAsync(readNotification, unreadNotification);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var actionResult = await _notificationController.GatherUnreadUserNotificationCountAsync();
            var okResult = actionResult.Result as ObjectResult;
            var response = okResult?.Value as ApiResponseDto<int?>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(1, response.Data);
        }

        [Fact]
        public async Task TestGatherUnreadUserNotificationCountAsync_ReturnsNotFoundForUnknownUser()
        {
            AuthenticateAsTrainer(999);
            var actionResult = await _notificationController.GatherUnreadUserNotificationCountAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;

            Assert.NotNull(notFoundResult);
        }
    }
}
