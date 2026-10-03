using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
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
        private readonly BookingSeriesRepository _bookingSeriesRepository;
        private readonly BookedSessionSlotRepository _bookedSessionSlotRepository;
        private readonly UnitOfWork _unitOfWork;

        public NotificationRepositoryTests()
        {
            _mapper = TestMapperFactory.Create();
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
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context, _mapper);
            _bookingSeriesRepository = new BookingSeriesRepository(_context);
            _bookedSessionSlotRepository = new BookedSessionSlotRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository, _bookingSeriesRepository, _bookedSessionSlotRepository);
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
                CommunicationType.Email,
                NotificationAudience.Trainer
            );
            await _unitOfWork.Complete();

            var savedNotification = await _context.Notification.FirstOrDefaultAsync();

            Assert.NotNull(savedNotification);
            Assert.Equal(trainer.Id, savedNotification.TrainerId);
            Assert.Equal(client.Id, savedNotification.ClientId);
            Assert.Equal("Test notification message", savedNotification.Message);
            Assert.Equal(NotificationType.TrainerBlockCompletionReminder, savedNotification.ReminderType);
            Assert.Equal(CommunicationType.Email, savedNotification.SentThrough);
            Assert.Equal(NotificationAudience.Trainer, savedNotification.Audience);
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
                CommunicationType.Sms,
                NotificationAudience.Trainer
            );
            await _unitOfWork.Complete();

            var savedNotification = await _context.Notification.FirstOrDefaultAsync();

            Assert.NotNull(savedNotification);
            Assert.Equal(trainer.Id, savedNotification.TrainerId);
            Assert.Null(savedNotification.ClientId);
            Assert.Equal("General notification", savedNotification.Message);
            Assert.Equal(NotificationType.NewClientConfigurationReminder, savedNotification.ReminderType);
            Assert.Equal(CommunicationType.Sms, savedNotification.SentThrough);
            Assert.Equal(NotificationAudience.Trainer, savedNotification.Audience);
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
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            _notificationRepository.DeleteNotification(notification);
            await _unitOfWork.Complete();

            Assert.False(_context.Notification.Any());
        }

        [Fact]
        public async Task TestReturnLatestUserNotifications_CapsAtTenAndOrdersByMostRecentFirst()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 12; i++)
            {
                var notification = new Notification
                {
                    TrainerId = trainer.Id,
                    Message = $"Notification {i}",
                    ReminderType = NotificationType.NewClientConfigurationReminder,
                    SentThrough = CommunicationType.InApp,
                    Audience = NotificationAudience.Trainer,
                    SentAt = baseTime.AddMinutes(-i)
                };
                notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });
                await _context.Notification.AddAsync(notification);
            }
            await _unitOfWork.Complete();

            var latest = await _notificationRepository.ReturnLatestUserNotificationDtosAsync(trainer);

            Assert.Equal(10, latest.Count);
            Assert.Equal("Notification 0", latest.First().Message);
            Assert.Equal("Notification 9", latest.Last().Message);
            for (int i = 0; i < latest.Count - 1; i++)
            {
                Assert.True(latest[i].SentAt >= latest[i + 1].SentAt);
            }
        }

        [Fact]
        public async Task TestReturnAllUserNotifications_ReturnsAllRecordsWithoutCap()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var baseTime = DateTime.UtcNow;
            for (int i = 0; i < 12; i++)
            {
                var notification = new Notification
                {
                    TrainerId = trainer.Id,
                    Message = $"Notification {i}",
                    ReminderType = NotificationType.NewClientConfigurationReminder,
                    SentThrough = CommunicationType.InApp,
                    Audience = NotificationAudience.Trainer,
                    SentAt = baseTime.AddMinutes(-i)
                };
                notification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });
                await _context.Notification.AddAsync(notification);
            }
            await _unitOfWork.Complete();

            var all = await _notificationRepository.ReturnAllUserNotificationDtosAsync(trainer);

            Assert.Equal(12, all.Count);
            Assert.Equal("Notification 0", all.First().Message);
        }

        [Fact]
        public async Task TestBuildUserNotificationQuery_TrainerOnlySeesOwnTrainerAudienceNotifications()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddRangeAsync(trainer, otherTrainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Belongs to the trainer and is addressed to them -- should be visible.
            var ownNotification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "Own",
                ReminderType = NotificationType.NewClientConfigurationReminder,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            ownNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });

            // Belongs to the trainer's own client relationship, but addressed to the client -- should NOT be visible to the trainer.
            var clientAudienceNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "For client",
                ReminderType = NotificationType.NewClientConfigurationReminder,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Client,
                SentAt = DateTime.UtcNow
            };
            clientAudienceNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = client.Id, IsRead = false });
            clientAudienceNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });

            // Belongs to a different trainer entirely -- should NOT be visible.
            var otherTrainerNotification = new Notification
            {
                TrainerId = otherTrainer.Id,
                Message = "Other trainer's",
                ReminderType = NotificationType.NewClientConfigurationReminder,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            otherTrainerNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = otherTrainer.Id, IsRead = false });

            await _context.Notification.AddRangeAsync(ownNotification, clientAudienceNotification, otherTrainerNotification);
            await _unitOfWork.Complete();

            var result = await _notificationRepository.ReturnAllUserNotificationDtosAsync(trainer);

            Assert.Single(result);
            Assert.Equal("Own", result.Single().Message);
        }

        [Fact]
        public async Task TestBuildUserNotificationQuery_ClientOnlySeesOwnClientAudienceNotifications()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "For client",
                ReminderType = NotificationType.ClientStepsTrackedNotification,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Client,
                SentAt = DateTime.UtcNow
            };
            clientNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = client.Id, IsRead = false });

            var trainerNotification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "For trainer",
                ReminderType = NotificationType.NewClientConfigurationReminder,
                SentThrough = CommunicationType.InApp,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            trainerNotification.RecipientStatuses.Add(new NotificationRecipientStatus { UserId = trainer.Id, IsRead = false });

            await _context.Notification.AddRangeAsync(clientNotification, trainerNotification);
            await _unitOfWork.Complete();

            var result = await _notificationRepository.ReturnAllUserNotificationDtosAsync(client);

            Assert.Single(result);
            Assert.Equal("For client", result.Single().Message);
        }

        [Fact]
        public async Task TestBuildUserNotificationQuery_IsReadReflectsRequestingUsersOwnStatus()
        {
            // Regression test: BuildUserNotificationQuery used to resolve IsRead via
            // RecipientStatuses.First(s => s.NotificationId == n.Id), which had no UserId filter and
            // could non-deterministically surface the OTHER recipient's read state on a shared notification.
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _notificationRepository.AddNotificationAsync(
                trainer.Id,
                client.Id,
                "Shared notification",
                NotificationType.TrainerBlockCompletionReminder,
                CommunicationType.Sms,
                NotificationAudience.Trainer
            );
            await _unitOfWork.Complete();

            var notification = await _context.Notification.Include(n => n.RecipientStatuses).SingleAsync();
            var trainerStatus = notification.RecipientStatuses.Single(s => s.UserId == trainer.Id);
            var clientStatus = notification.RecipientStatuses.Single(s => s.UserId == client.Id);

            // Client has read it, trainer has not -- the trainer's own view must still show unread.
            clientStatus.IsRead = true;
            trainerStatus.IsRead = false;
            await _unitOfWork.Complete();

            var beforeTrainerReads = await _notificationRepository.ReturnLatestUserNotificationDtosAsync(trainer);
            Assert.False(beforeTrainerReads.Single().IsRead);

            // Flip: trainer has now read it, client has not.
            trainerStatus.IsRead = true;
            clientStatus.IsRead = false;
            await _unitOfWork.Complete();

            var afterTrainerReads = await _notificationRepository.ReturnLatestUserNotificationDtosAsync(trainer);
            Assert.True(afterTrainerReads.Single().IsRead);
        }
    }
}

