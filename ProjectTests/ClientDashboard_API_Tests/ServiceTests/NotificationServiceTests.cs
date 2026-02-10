using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // Fake message service for testing
    public class FakeMessageService : IMessageService
    {
        private readonly List<string> _sentMessages = new();

        public List<string> SentMessages => _sentMessages;

        public void InitialiseBaseTwillioClient()
        {
            // Fake implementation - no actual Twilio client needed for tests
        }

        public void PipelineClientBlockCompletionReminder(string clientName)
        {
            // Fake implementation for pipeline reminders
            _sentMessages.Add($"Pipeline reminder for client: {clientName}");
        }

        public void SendSMSMessage(Trainer? trainer, Client? client, string senderPhoneNumber, string message)
        {
            // Simulate sending SMS by storing the message
            _sentMessages.Add($"SMS to {trainer?.PhoneNumber ?? client?.PhoneNumber}: {message}");
        }
    }

    public class NotificationServiceTests
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly EmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;
        private readonly FakeMessageService _fakeMessageService;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            });
            _mapper = config.CreateMapper();
            _passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _userRepository = new UserRepository(_context, _passwordHasher);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            _fakeMessageService = new FakeMessageService();
            _notificationService = new NotificationService(_unitOfWork, _fakeMessageService);

            // Set environment variable for tests
            Environment.SetEnvironmentVariable("SENDER_PHONE_NUMBER", "+1234567890");
        }

        #region SendTrainerBlockReminderAsync Tests

        [Fact]
        public async Task TestSendTrainerBlockReminderSendsInAppNotificationSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerBlockReminderAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("john", result.Data);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.InApp, notifications[0].SentThrough);
            Assert.Equal(NotificationType.TrainerBlockCompletionReminder, notifications[0].ReminderType);
        }

        [Fact]
        public async Task TestSendTrainerBlockReminderSendsSMSWhenEnabledAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                PhoneNumber = "+1234567890",
                NotificationsEnabled = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerBlockReminderAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(_fakeMessageService.SentMessages);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.Sms, notifications[0].SentThrough);
        }

        [Fact]
        public async Task TestSendTrainerBlockReminderReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerBlockReminderAsync(999, client.Id);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestSendTrainerBlockReminderReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerBlockReminderAsync(trainer.Id, 999);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        #endregion

        #region SendClientBlockReminderAsync Tests

        [Fact]
        public async Task TestSendClientBlockReminderSendsInAppNotificationSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8,
                NotificationsEnabled = false
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendClientBlockReminderAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.InApp, notifications[0].SentThrough);
            Assert.Equal(NotificationType.ClientBlockCompletionReminder, notifications[0].ReminderType);
        }

        [Fact]
        public async Task TestSendClientBlockReminderSendsSMSWhenEnabledAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8,
                NotificationsEnabled = true
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendClientBlockReminderAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(_fakeMessageService.SentMessages);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.Sms, notifications[0].SentThrough);
        }

        [Fact]
        public async Task TestSendClientBlockReminderReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendClientBlockReminderAsync(999, client.Id);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestSendClientBlockReminderReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendClientBlockReminderAsync(trainer.Id, 999);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        #endregion

        #region SendTrainerPendingPaymentAlertAsync Tests

        [Fact]
        public async Task TestSendTrainerPendingPaymentAlertSendsInAppNotificationSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerPendingPaymentAlertAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.InApp, notifications[0].SentThrough);
            Assert.Equal(NotificationType.PendingPaymentCreatedAlert, notifications[0].ReminderType);
        }

        [Fact]
        public async Task TestSendTrainerPendingPaymentAlertSendsSMSWhenEnabledAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                PhoneNumber = "+1234567890",
                NotificationsEnabled = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerPendingPaymentAlertAsync(trainer.Id, client.Id);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(_fakeMessageService.SentMessages);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.Sms, notifications[0].SentThrough);
        }

        [Fact]
        public async Task TestSendTrainerPendingPaymentAlertReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerPendingPaymentAlertAsync(999, client.Id);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task TestSendTrainerPendingPaymentAlertReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _notificationService.SendTrainerPendingPaymentAlertAsync(trainer.Id, 999);

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        #endregion

        #region SendTrainerAutoWorkoutCollectionNoticeAsync Tests

        [Fact]
        public async Task TestSendTrainerAutoWorkoutCollectionNoticeSendsInAppNotificationSuccessfullyAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = DateTime.UtcNow;
            var result = await _notificationService.SendTrainerAutoWorkoutCollectionNoticeAsync(trainer, 5, date);

            Assert.NotNull(result);
            Assert.True(result.Success);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.InApp, notifications[0].SentThrough);
            Assert.Equal(NotificationType.AutoRetrievalWorkoutsCountNotification, notifications[0].ReminderType);
            Assert.Null(notifications[0].ClientId);
        }

        [Fact]
        public async Task TestSendTrainerAutoWorkoutCollectionNoticeSendsSMSWhenEnabledAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                PhoneNumber = "+1234567890",
                NotificationsEnabled = true
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = DateTime.UtcNow;
            var result = await _notificationService.SendTrainerAutoWorkoutCollectionNoticeAsync(trainer, 10, date);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(_fakeMessageService.SentMessages);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal(CommunicationType.Sms, notifications[0].SentThrough);
        }

        [Fact]
        public async Task TestSendTrainerAutoWorkoutCollectionNoticeHandlesZeroWorkoutsAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = DateTime.UtcNow;
            var result = await _notificationService.SendTrainerAutoWorkoutCollectionNoticeAsync(trainer, 0, date);

            Assert.NotNull(result);
            Assert.True(result.Success);

            var notifications = await _context.Notification.ToListAsync();
            Assert.Single(notifications);
        }

        #endregion

        #region Notification Message Content Tests

        [Fact]
        public async Task TestTrainerBlockReminderMessageContainsClientNameAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationService.SendTrainerBlockReminderAsync(trainer.Id, client.Id);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains("alice", notification.Message.ToLower());
        }

        [Fact]
        public async Task TestClientBlockReminderMessageContainsClientNameAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8,
                NotificationsEnabled = false
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationService.SendClientBlockReminderAsync(trainer.Id, client.Id);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains("alice", notification.Message.ToLower());
        }

        [Fact]
        public async Task TestWorkoutCollectionMessageContainsWorkoutCountAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                NotificationsEnabled = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = DateTime.UtcNow;
            await _notificationService.SendTrainerAutoWorkoutCollectionNoticeAsync(trainer, 15, date);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains("15", notification.Message);
        }

        #endregion
    }
}
