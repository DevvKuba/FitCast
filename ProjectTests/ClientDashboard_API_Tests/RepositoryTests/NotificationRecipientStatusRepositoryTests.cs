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
        private readonly BookingSeriesRepository _bookingSeriesRepository;
        private readonly BookedSessionSlotRepository _bookedSessionSlotRepository;
        private readonly UnitOfWork _unitOfWork;

        public NotificationRecipientStatusRepositoryTests()
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
            _notificationRecipientStatusRepository = new NotificationRecipientStatusRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context, _mapper);
            _bookingSeriesRepository = new BookingSeriesRepository(_context);
            _bookedSessionSlotRepository = new BookedSessionSlotRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _notificationRecipientStatusRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository, _bookingSeriesRepository, _bookedSessionSlotRepository);
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
        public async Task TestMarkUserNotificationsAsReadAsync()
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

            await _notificationRecipientStatusRepository.MarkUserNotificationsAsReadAsync(trainer.Id, [n1.Id]);
            await _unitOfWork.Complete();

            var updatedS1 = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == s1.Id);
            var updatedS2 = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == s2.Id);

            Assert.True(updatedS1.IsRead);
            Assert.NotNull(updatedS1.ReadAt);
            Assert.False(updatedS2.IsRead);
            Assert.Null(updatedS2.ReadAt);
        }

        [Fact]
        public async Task TestMarkUserNotificationsAsReadAsync_DoesNotAffectAnotherUsersStatusForTheSameNotification()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "shared",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            var trainerStatus = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = notification.Id, IsRead = false };
            var clientStatus = new NotificationRecipientStatus { UserId = client.Id, NotificationId = notification.Id, IsRead = false };
            await _context.NotificationRecipientStatuses.AddRangeAsync(trainerStatus, clientStatus);
            await _unitOfWork.Complete();

            await _notificationRecipientStatusRepository.MarkUserNotificationsAsReadAsync(trainer.Id, [notification.Id]);
            await _unitOfWork.Complete();

            var updatedTrainerStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == trainerStatus.Id);
            var updatedClientStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == clientStatus.Id);

            Assert.True(updatedTrainerStatus.IsRead);
            Assert.False(updatedClientStatus.IsRead);
        }

        [Fact]
        public async Task TestMarkNotificationsAsReadAsync_MarksEveryRecipientOfTheGivenNotifications()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var expiredNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "expired",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-20)
            };

            var otherNotification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "not targeted",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-20)
            };

            await _context.Notification.AddRangeAsync(expiredNotification, otherNotification);
            await _unitOfWork.Complete();

            // The system-wide overload has no user context, so it must flip every recipient's row
            // for the given notification ids - both the trainer's and the client's.
            var expiredTrainerStatus = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = expiredNotification.Id, IsRead = false };
            var expiredClientStatus = new NotificationRecipientStatus { UserId = client.Id, NotificationId = expiredNotification.Id, IsRead = false };
            var untouchedStatus = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = otherNotification.Id, IsRead = false };
            await _context.NotificationRecipientStatuses.AddRangeAsync(expiredTrainerStatus, expiredClientStatus, untouchedStatus);
            await _unitOfWork.Complete();

            await _notificationRecipientStatusRepository.MarkNotificationsAsReadAsync([expiredNotification.Id]);
            await _unitOfWork.Complete();

            var updatedTrainerStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == expiredTrainerStatus.Id);
            var updatedClientStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == expiredClientStatus.Id);
            var updatedUntouchedStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == untouchedStatus.Id);

            Assert.True(updatedTrainerStatus.IsRead);
            Assert.NotNull(updatedTrainerStatus.ReadAt);
            Assert.True(updatedClientStatus.IsRead);
            Assert.NotNull(updatedClientStatus.ReadAt);
            Assert.False(updatedUntouchedStatus.IsRead);
            Assert.Null(updatedUntouchedStatus.ReadAt);
        }

        [Fact]
        public async Task TestMarkNotificationsAsReadAsync_LeavesAlreadyReadRowsUntouched()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var notification = new Notification
            {
                TrainerId = trainer.Id,
                Message = "already read",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-20)
            };
            await _context.Notification.AddAsync(notification);
            await _unitOfWork.Complete();

            var readAt = DateTime.UtcNow.AddDays(-19);
            var status = new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = notification.Id, IsRead = true, ReadAt = readAt };
            await _context.NotificationRecipientStatuses.AddAsync(status);
            await _unitOfWork.Complete();

            await _notificationRecipientStatusRepository.MarkNotificationsAsReadAsync([notification.Id]);
            await _unitOfWork.Complete();

            var updatedStatus = await _context.NotificationRecipientStatuses.FirstAsync(x => x.Id == status.Id);

            Assert.True(updatedStatus.IsRead);
            Assert.Equal(readAt, updatedStatus.ReadAt);
        }

        [Fact]
        public async Task TestGetExpiredUnreadNotificationIdsAsync_ReturnsOnlyUnreadNotificationsOlderThanFourteenDays()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var expiredUnread = new Notification
            {
                TrainerId = trainer.Id,
                Message = "expired unread",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-15)
            };

            var expiredButRead = new Notification
            {
                TrainerId = trainer.Id,
                Message = "expired but already read",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-15)
            };

            var notYetExpired = new Notification
            {
                TrainerId = trainer.Id,
                Message = "still within the window",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-10)
            };

            await _context.Notification.AddRangeAsync(expiredUnread, expiredButRead, notYetExpired);
            await _unitOfWork.Complete();

            await _context.NotificationRecipientStatuses.AddRangeAsync(
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = expiredUnread.Id, IsRead = false },
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = expiredButRead.Id, IsRead = true, ReadAt = DateTime.UtcNow.AddDays(-14) },
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = notYetExpired.Id, IsRead = false }
            );
            await _unitOfWork.Complete();

            var expiredIds = await _notificationRecipientStatusRepository.GetExpiredUnreadNotificationIdsAsync();

            Assert.Equal([expiredUnread.Id], expiredIds);
        }

        [Fact]
        public async Task TestGetExpiredUnreadNotificationIdsAsync_ReturnsOneEntryPerUnreadExpiredRecipient()
        {
            // A notification with two recipients (trainer + client) who are both still unread past
            // the window shows up twice - once per expired NotificationRecipientStatus row, since
            // the query operates at the recipient-status level, not the notification level.
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var sharedNotification = new Notification
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Message = "shared expired",
                ReminderType = NotificationType.TrainerBlockCompletionReminder,
                SentThrough = CommunicationType.Email,
                Audience = NotificationAudience.Trainer,
                SentAt = DateTime.UtcNow.AddDays(-15)
            };
            await _context.Notification.AddAsync(sharedNotification);
            await _unitOfWork.Complete();

            await _context.NotificationRecipientStatuses.AddRangeAsync(
                new NotificationRecipientStatus { UserId = trainer.Id, NotificationId = sharedNotification.Id, IsRead = false },
                new NotificationRecipientStatus { UserId = client.Id, NotificationId = sharedNotification.Id, IsRead = false }
            );
            await _unitOfWork.Complete();

            var expiredIds = await _notificationRecipientStatusRepository.GetExpiredUnreadNotificationIdsAsync();

            Assert.Equal(2, expiredIds.Count);
            Assert.All(expiredIds, id => Assert.Equal(sharedNotification.Id, id));
        }
    }
}
