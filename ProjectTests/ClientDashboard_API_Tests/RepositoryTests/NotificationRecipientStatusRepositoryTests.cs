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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class NotificationRecipientStatusRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly NotificationRecipientStatusRepository _notificationRecipientStatusRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly EmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;

        public NotificationRecipientStatusRepositoryTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            }, global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
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
            _notificationRecipientStatusRepository = new NotificationRecipientStatusRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _notificationRecipientStatusRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);
        }

        [Fact]
        public async Task TestGetNotificationRecipientStatusByIdAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "status lookup",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            var status = new NotificationRecipientStatus
            {
                UserId = trainer.Id,
                NotificationId = notification.Id,
                IsRead = false
            };
            await _context.NotificationRecipientStatuses.AddAsync(status);
            await _unitOfWork.Complete();

            var result = await _notificationRecipientStatusRepository.GetNotificationRecipientStatusByIdAsync(status.Id);

            Assert.NotNull(result);
            Assert.Equal(status.Id, result.Id);
            Assert.Equal(trainer.Id, result.UserId);
            Assert.Equal(notification.Id, result.NotificationId);
        }

        [Fact]
        public async Task TestGetLatestUserNotificationStatusesAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            for (int i = 0; i < 12; i++)
            {
                var notification = new Notification
                {
                    TrainerId = trainer.Id,
                    Message = $"notification {i}",
                    ReminderType = NotificationType.TrainerBlockCompletionReminder,
                    SentThrough = CommunicationType.Email,
                    Audience = NotificationAudience.Trainer,
                    SentAt = DateTime.UtcNow.AddMinutes(-i)
                };

                await _context.Notification.AddAsync(notification);
                await _unitOfWork.Complete();

                await _context.NotificationRecipientStatuses.AddAsync(new NotificationRecipientStatus
                {
                    UserId = trainer.Id,
                    NotificationId = notification.Id,
                    IsRead = false
                });
            }

            await _unitOfWork.Complete();

            var latestStatuses = await _notificationRecipientStatusRepository.GetLatestUserNotificationStatusesAsync(trainer.Id);

            Assert.Equal(10, latestStatuses.Count);
            Assert.All(latestStatuses, s => Assert.NotNull(s.Notification));
            Assert.True(latestStatuses.Zip(latestStatuses.Skip(1), (a, b) => a.Notification.SentAt >= b.Notification.SentAt).All(x => x));
        }

        [Fact]
        public async Task TestGetUnreadUserNotificationCountAsyncForTrainerFiltersByAudience()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var trainerAudienceNotification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "trainer audience",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            var clientAudienceNotification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "client audience",
                ReminderType = NotificationType.ClientBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Client,
                SentAt = DateTime.UtcNow
            };

            await _context.Notification.AddRangeAsync(trainerAudienceNotification, clientAudienceNotification);
            await _unitOfWork.Complete();

            await _context.NotificationRecipientStatuses.AddRangeAsync(
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = trainerAudienceNotification.Id, IsRead = false },
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = clientAudienceNotification.Id, IsRead = false }
            );
            await _unitOfWork.Complete();

            var unreadCount = await _notificationRecipientStatusRepository.GetUnreadUserNotificationCountAsync(trainer);

            Assert.Equal(1, unreadCount);
        }

        [Fact]
        public async Task TestGetUnreadUserNotificationCountAsyncForClientFiltersByAudience()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientAudienceNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "client audience",
                ReminderType = NotificationType.ClientBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Client,
                SentAt = DateTime.UtcNow
            };

            var trainerAudienceNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "trainer audience",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            await _context.Notification.AddRangeAsync(clientAudienceNotification, trainerAudienceNotification);
            await _unitOfWork.Complete();

            await _context.NotificationRecipientStatuses.AddRangeAsync(
                new NotificationRecipientStatus { UserId = client.Id, NotificationId = clientAudienceNotification.Id, IsRead = false },
                new NotificationRecipientStatus { UserId = client.Id, NotificationId = trainerAudienceNotification.Id, IsRead = false }
            );
            await _unitOfWork.Complete();

            var unreadCount = await _notificationRecipientStatusRepository.GetUnreadUserNotificationCountAsync(client);

            Assert.Equal(1, unreadCount);
        }

        [Fact]
        public async Task TestMarkNotificationsAsReadAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var n1 = new Notification
            {
                TrainerId = trainer.Id,
                Message = "n1",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            var n2 = new Notification
            {
                TrainerId = trainer.Id,
                Message = "n2",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            await _context.Notification.AddRangeAsync(n1, n2);
            await _unitOfWork.Complete();

            var s1 = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = n1.Id, IsRead = false };
            var s2 = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = n2.Id, IsRead = false };
            await _context.NotificationRecipientStatuses.AddRangeAsync(s1, s2);
            await _unitOfWork.Complete();

            await _notificationRecipientStatusRepository.MarkNotificationsAsReadAsync(trainer.Id, [n1.Id]);
            await _unitOfWork.Complete();

            var updatedS1 = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == s1.Id);
            var updatedS2 = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == s2.Id);

            Assert.True(updatedS1.IsRead);
            Assert.NotNull(updatedS1.ReadAt);
            Assert.False(updatedS2.IsRead);
            Assert.Null(updatedS2.ReadAt);
        }

        [Fact]
        public async Task TestAddNotificationRecipientStatusAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "single status",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            await _notificationRecipientStatusRepository.AddNotificationRecipientStatusAsync(trainer.Id, notification.Id);
            await _unitOfWork.Complete();

            var status = await _context.NotificationRecipientStatuses.FirstOrDefaultAsync();

            Assert.NotNull(status);
            Assert.Equal(trainer.Id, status!.UserId);
            Assert.Equal(notification.Id, status.NotificationId);
            Assert.False(status.IsRead);
        }

        [Fact]
        public async Task TestAddNotificationRecipientStatusesAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var n1 = new Notification
            {
                TrainerId = trainer.Id,
                Message = "bulk1",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            var n2 = new Notification
            {
                TrainerId = trainer.Id,
                Message = "bulk2",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };

            await _context.Notification.AddRangeAsync(n1, n2);
            await _unitOfWork.Complete();

            var statuses = new List<NotificationRecipientStatus>
            {
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = n1.Id },
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = n2.Id }
            };

            await _notificationRecipientStatusRepository.AddNotificationRecipientStatusesAsync(statuses);
            await _unitOfWork.Complete();

            Assert.Equal(2, _context.NotificationRecipientStatuses.Count());
            Assert.Contains(_context.NotificationRecipientStatuses, s => s.NotificationId == n1.Id);
            Assert.Contains(_context.NotificationRecipientStatuses, s => s.NotificationId == n2.Id);
        }

        [Fact]
        public async Task TestDeleteNotificationRecipientStatus()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "delete status",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            var status = new NotificationRecipientStatus
            {
                UserId = trainer.Id,
                NotificationId = notification.Id
            };
            await _context.NotificationRecipientStatuses.AddAsync(status);
            await _unitOfWork.Complete();

            _notificationRecipientStatusRepository.DeleteNotificationRecipientStatus(status);
            await _unitOfWork.Complete();

            Assert.False(_context.NotificationRecipientStatuses.Any());
        }
    }
}
