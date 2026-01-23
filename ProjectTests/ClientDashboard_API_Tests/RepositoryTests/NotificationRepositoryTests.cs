using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class NotificationRepositoryTests
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

        public NotificationRepositoryTests()
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
        }

        [Fact]
        public async Task TestAddNotificationWithClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            var client = new Client
            {
                FirstName = "rob",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationRepository.AddNotificationAsync(
                trainer.Id,
                client.Id,
                "Test notification message",
                NotificationType.TrainerBlockCompletionReminder,
                CommunicationType.Email
            );
            await _unitOfWork.Complete();

            var savedNotification = await _context.Notification.FirstOrDefaultAsync();

            Assert.NotNull(savedNotification);
            Assert.Equal(trainer.Id, savedNotification.TrainerId);
            Assert.Equal(client.Id, savedNotification.ClientId);
            Assert.Equal("Test notification message", savedNotification.Message);
            Assert.Equal(NotificationType.TrainerBlockCompletionReminder, savedNotification.ReminderType);
            Assert.Equal(CommunicationType.Email, savedNotification.SentThrough);
            Assert.True(savedNotification.SentAt <= DateTime.UtcNow);
            Assert.True(savedNotification.SentAt >= DateTime.UtcNow.AddSeconds(-5));
        }

        [Fact]
        public async Task TestAddNotificationWithoutClientAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _notificationRepository.AddNotificationAsync(
                trainer.Id,
                null,
                "General notification",
                NotificationType.NewClientConfigurationReminder,
                CommunicationType.Sms
            );
            await _unitOfWork.Complete();

            var savedNotification = await _context.Notification.FirstOrDefaultAsync();

            Assert.NotNull(savedNotification);
            Assert.Equal(trainer.Id, savedNotification.TrainerId);
            Assert.Null(savedNotification.ClientId);
            Assert.Equal("General notification", savedNotification.Message);
            Assert.Equal(NotificationType.NewClientConfigurationReminder, savedNotification.ReminderType);
            Assert.Equal(CommunicationType.Sms, savedNotification.SentThrough);
        }

        [Fact]
        public async Task TestDeleteNotificationAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = null,
                Message = "Test message",
                ReminderType = NotificationType.ClientBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            _notificationRepository.DeleteNotification(notification);
            await _unitOfWork.Complete();

            Assert.False(_context.Notification.Any());
        }
    }
}
