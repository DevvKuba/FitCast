using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class NotificationControllerTests
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
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;
        private readonly TestTwillioMessageService _messageService;
        private readonly NotificationService _notificationService;
        private readonly NotificationController _notificationController;

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
            var config = new MapperConfiguration(cfg =>
            {
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
            _userRepository = new UserRepository(_context);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, 
                _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, 
                _clientDailyFeatureRepository, _trainerDailyRevenueRepository);


            _messageService = new TestTwillioMessageService();
            _notificationService = new NotificationService(_unitOfWork, _messageService);
            _notificationController = new NotificationController(_notificationService);
        }

        [Fact]
        public async Task TestSuccessfullySendingTrainerBlockCompletionReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(trainer.Id, client.Id);
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
            Assert.Equal("Trainer Client Block termination", notification.ReminderType);
            Assert.Equal("SMS", notification.SentThrough);
        }

        [Fact]
        public async Task TestUnsuccessfullySendingTrainerBlockCompletionReminderWithInvalidTrainerIdAsync()
        {
            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var invalidTrainerId = 999;

            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(invalidTrainerId, client.Id);
            var badRequestResult = actionResult.Result as ObjectResult;
            var response = badRequestResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains($"Trainer with id: {invalidTrainerId}", response.Message);
        }

        [Fact]
        public async Task TestUnsuccessfullySendingTrainerBlockCompletionReminderWithInvalidClientIdAsync()
        {
            var trainer = new Trainer
            {
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var invalidClientId = 999;

            var actionResult = await _notificationController.TrainerBlockCompletionReminderAsync(trainer.Id, invalidClientId);
            var badRequestResult = actionResult.Result as ObjectResult;
            var response = badRequestResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains($"Client with id: {invalidClientId}", response.Message);
        }

        [Fact]
        public async Task TestSuccessfullySendingClientBlockCompletionReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(trainer.Id, client.Id);
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
            Assert.Equal("Trainer Client Block termination", notification.ReminderType);
            Assert.Equal("SMS", notification.SentThrough);
        }

        [Fact]
        public async Task TestUnsuccessfullySendingClientBlockCompletionReminderWithInvalidTrainerIdAsync()
        {
            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var invalidTrainerId = 999;

            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(invalidTrainerId, client.Id);
            var badRequestResult = actionResult.Result as ObjectResult;
            var response = badRequestResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains($"Trainer with id: {invalidTrainerId}", response.Message);
        }

        [Fact]
        public async Task TestUnsuccessfullySendingClientBlockCompletionReminderWithInvalidClientIdAsync()
        {
            var trainer = new Trainer
            {
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var invalidClientId = 999;

            var actionResult = await _notificationController.ClientBlockCompletionReminderAsync(trainer.Id, invalidClientId);
            var badRequestResult = actionResult.Result as ObjectResult;
            var response = badRequestResult?.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains($"Client with id: {invalidClientId}", response.Message);
        }

        [Fact]
        public async Task TestNotificationIsStoredInDatabaseAfterTrainerReminderAsync()
        {
            var trainer = new Trainer
            {
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationController.TrainerBlockCompletionReminderAsync(trainer.Id, client.Id);

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
                Role = "trainer",
                FirstName = "John",
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "+1234567890",
                PasswordHash = "hash123"
            };

            var client = new Client
            {
                Role = "client",
                FirstName = "Jane",
                Surname = "Smith",
                PhoneNumber = "+0987654321",
                CurrentBlockSession = 8,
                TotalBlockSessions = 8
            };

            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationController.ClientBlockCompletionReminderAsync(trainer.Id, client.Id);

            var notificationCount = await _context.Notification.CountAsync();
            Assert.Equal(1, notificationCount);

            var notification = await _context.Notification.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Contains(client.FirstName, notification.Message);
            Assert.Contains("monthly sessions", notification.Message);
        }
    }
}
