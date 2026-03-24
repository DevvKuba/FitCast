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
using System.Globalization;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class NotificationMessageHelperTests
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

        public NotificationMessageHelperTests()
        {
            // Set culture to en-GB to match production environment
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
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
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);
        }

        [Fact]
        public void TestGetMessageReturnsTrainerBlockCompletionReminderCorrectly()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john.doe@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "alice",
                Email = "alice@example.com",
                Role = UserRole.Client
            };

            // Act
            var message = NotificationMessageHelper.GetMessage(
                NotificationType.TrainerBlockCompletionReminder,
                trainer,
                client
            );

            // Assert
            Assert.NotNull(message);
            Assert.Contains("alice's monthly sessions have come to an end", message);
            Assert.Contains("remember to message them in regards of a new monthly payment", message);
        }

        [Fact]
        public void TestGetMessageReturnsClientBlockCompletionReminderCorrectly()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "smith",
                Email = "jane.smith@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "bob",
                Email = "bob@example.com",
                Role = UserRole.Client
            };

            // Act
            var message = NotificationMessageHelper.GetMessage(
                NotificationType.ClientBlockCompletionReminder,
                trainer,
                client
            );

            // Assert
            Assert.NotNull(message);
            Assert.Contains("Hey bob!", message);
            Assert.Contains("our monthly sessions have come to an end", message);
            Assert.Contains("place a block payment before our next session block", message);
        }

        [Fact]
        public void TestGetMessageReturnsPendingPaymentCreatedAlertCorrectly()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "charlie",
                Surname = "brown",
                Email = "charlie.brown@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "david",
                Email = "david@example.com",
                Role = UserRole.Client,
                TotalBlockSessions = 8
            };

            // Act
            var message = NotificationMessageHelper.GetMessage(
                NotificationType.PendingPaymentCreatedAlert,
                trainer,
                client
            );

            // Assert
            Assert.NotNull(message);
            Assert.Contains("Pending payment for a block of 8 sessions", message);
            Assert.Contains("created for david", message);
        }

        [Fact]
        public void TestGetMessageUsesClientNameInMessagesCorrectly()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "eve",
                Surname = "wilson",
                Email = "eve.wilson@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "Maximilian",
                Email = "max@example.com",
                Role = UserRole.Client,
                TotalBlockSessions = 10
            };

            // Act
            var trainerMessage = NotificationMessageHelper.GetMessage(
                NotificationType.TrainerBlockCompletionReminder,
                trainer,
                client
            );
            var clientMessage = NotificationMessageHelper.GetMessage(
                NotificationType.ClientBlockCompletionReminder,
                trainer,
                client
            );
            var paymentMessage = NotificationMessageHelper.GetMessage(
                NotificationType.PendingPaymentCreatedAlert,
                trainer,
                client
            );

            // Assert
            Assert.Contains("Maximilian", trainerMessage);
            Assert.Contains("Maximilian", clientMessage);
            Assert.Contains("Maximilian", paymentMessage);
        }

        [Fact]
        public void TestGetMessageThrowsArgumentExceptionForInvalidNotificationType()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "frank",
                Surname = "garcia",
                Email = "frank.garcia@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "grace",
                Email = "grace@example.com",
                Role = UserRole.Client
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                NotificationMessageHelper.GetMessage(
                    (NotificationType)999, // Invalid type
                    trainer,
                    client
                )
            );

            Assert.Contains("No message template found for notification type", exception.Message);
        }

        [Fact]
        public void TestGetMessageIncludesTotalBlockSessionsInPendingPaymentAlert()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "henry",
                Surname = "davis",
                Email = "henry.davis@example.com",
                Role = UserRole.Trainer
            };

            var client1 = new Client
            {
                FirstName = "iris",
                Email = "iris@example.com",
                Role = UserRole.Client,
                TotalBlockSessions = 5
            };

            var client2 = new Client
            {
                FirstName = "jack",
                Email = "jack@example.com",
                Role = UserRole.Client,
                TotalBlockSessions = 20
            };

            // Act
            var message1 = NotificationMessageHelper.GetMessage(
                NotificationType.PendingPaymentCreatedAlert,
                trainer,
                client1
            );
            var message2 = NotificationMessageHelper.GetMessage(
                NotificationType.PendingPaymentCreatedAlert,
                trainer,
                client2
            );

            // Assert
            Assert.Contains("5 sessions", message1);
            Assert.Contains("20 sessions", message2);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageReturnsCorrectMessageForZeroWorkouts()
        {
            // Arrange
            var date = new DateTime(2025, 3, 15, 14, 30, 0);

            // Act
            var message = NotificationMessageHelper.GetWorkoutCollectionMessage(0, date);

            // Assert
            Assert.Contains("No new workouts retrieved from Hevy", message);
            Assert.Contains("Friday", message); // March 14, 2025 (date - 1 day) is FRIDAY
            Assert.Contains("15th", message);
            Assert.Contains("2:30 PM", message);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageReturnsCorrectMessageForOneWorkout()
        {
            // Arrange
            var date = new DateTime(2025, 3, 10, 9, 15, 0);

            // Act
            var message = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date);

            // Assert
            Assert.Contains("1 new workout retrieved from Hevy", message);
            Assert.Contains("Sunday", message); // March 9, 2025 (date - 1 day)
            Assert.Contains("10th", message);
            Assert.Contains("9:15 AM", message);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageReturnsCorrectMessageForMultipleWorkouts()
        {
            // Arrange
            var date = new DateTime(2025, 3, 22, 18, 45, 0);

            // Act
            var message = NotificationMessageHelper.GetWorkoutCollectionMessage(5, date);

            // Assert
            Assert.Contains("5 new workouts retrieved from Hevy", message);
            Assert.Contains("Friday", message); // March 21, 2025 (date - 1 day)
            Assert.Contains("22nd", message);
            Assert.Contains("6:45 PM", message);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageFormatsOrdinalDaysCorrectly()
        {
            // Test 1st, 2nd, 3rd, and th suffixes
            var date1 = new DateTime(2025, 1, 1, 12, 0, 0);
            var date2 = new DateTime(2025, 1, 2, 12, 0, 0);
            var date3 = new DateTime(2025, 1, 3, 12, 0, 0);
            var date4 = new DateTime(2025, 1, 4, 12, 0, 0);
            var date21 = new DateTime(2025, 1, 21, 12, 0, 0);
            var date22 = new DateTime(2025, 1, 22, 12, 0, 0);
            var date23 = new DateTime(2025, 1, 23, 12, 0, 0);
            var date31 = new DateTime(2025, 1, 31, 12, 0, 0);

            var message1 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date1);
            var message2 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date2);
            var message3 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date3);
            var message4 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date4);
            var message21 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date21);
            var message22 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date22);
            var message23 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date23);
            var message31 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date31);

            Assert.Contains("1st", message1);
            Assert.Contains("2nd", message2);
            Assert.Contains("3rd", message3);
            Assert.Contains("4th", message4);
            Assert.Contains("21st", message21);
            Assert.Contains("22nd", message22);
            Assert.Contains("23rd", message23);
            Assert.Contains("31st", message31);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageHandlesLargeWorkoutCount()
        {
            // Arrange
            var date = new DateTime(2025, 12, 25, 15, 30, 0);

            // Act
            var message = NotificationMessageHelper.GetWorkoutCollectionMessage(100, date);

            // Assert
            Assert.Contains("100 new workouts retrieved from Hevy", message);
            Assert.Contains("Wednesday", message); // December 24, 2025
            Assert.Contains("25th", message);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageSubtractsOneDayCorrectly()
        {
            // Arrange - Test that date.AddDays(-1) is used correctly
            var date = new DateTime(2025, 4, 1, 10, 0, 0); // April 1st

            // Act
            var message = NotificationMessageHelper.GetWorkoutCollectionMessage(3, date);

            // Assert
            Assert.Contains("Monday", message); // March 31, 2025 (date - 1 day)
            Assert.Contains("1st", message); // Still shows the original date day for ordinal
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageFormats12HourTimeCorrectly()
        {
            // Test AM and PM formatting
            var dateAM = new DateTime(2025, 6, 15, 8, 30, 0);
            var datePM = new DateTime(2025, 6, 15, 20, 45, 0);
            var dateNoon = new DateTime(2025, 6, 15, 12, 0, 0);
            var dateMidnight = new DateTime(2025, 6, 15, 0, 0, 0);

            var messageAM = NotificationMessageHelper.GetWorkoutCollectionMessage(1, dateAM);
            var messagePM = NotificationMessageHelper.GetWorkoutCollectionMessage(1, datePM);
            var messageNoon = NotificationMessageHelper.GetWorkoutCollectionMessage(1, dateNoon);
            var messageMidnight = NotificationMessageHelper.GetWorkoutCollectionMessage(1, dateMidnight);

            Assert.Contains("8:30 AM", messageAM);
            Assert.Contains("8:45 PM", messagePM);
            Assert.Contains("12:00 PM", messageNoon);
            Assert.Contains("12:00 AM", messageMidnight);
        }

        [Fact]
        public void TestGetMessageHandlesNullTotalBlockSessionsInPendingPaymentAlert()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "kate",
                Surname = "anderson",
                Email = "kate.anderson@example.com",
                Role = UserRole.Trainer
            };

            var client = new Client
            {
                FirstName = "leo",
                Email = "leo@example.com",
                Role = UserRole.Client,
                TotalBlockSessions = null // Null sessions
            };

            // Act
            var message = NotificationMessageHelper.GetMessage(
                NotificationType.PendingPaymentCreatedAlert,
                trainer,
                client
            );

            // Assert
            Assert.NotNull(message);
            Assert.Contains("Pending payment for a block of", message);
            Assert.Contains("sessions", message);
            // Should still work even with null (C# will convert null to empty string in interpolation)
        }

        [Fact]
        public void TestGetMessageWorksWithDifferentClientNamesIncludingSpecialCharacters()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "mike",
                Surname = "davis",
                Email = "mike.davis@example.com",
                Role = UserRole.Trainer
            };

            var client1 = new Client { FirstName = "O'Brien", Email = "obrien@example.com", Role = UserRole.Client };
            var client2 = new Client { FirstName = "José", Email = "jose@example.com", Role = UserRole.Client };
            var client3 = new Client { FirstName = "Anne-Marie", Email = "anne@example.com", Role = UserRole.Client };

            // Act
            var message1 = NotificationMessageHelper.GetMessage(NotificationType.TrainerBlockCompletionReminder, trainer, client1);
            var message2 = NotificationMessageHelper.GetMessage(NotificationType.ClientBlockCompletionReminder, trainer, client2);
            var message3 = NotificationMessageHelper.GetMessage(NotificationType.TrainerBlockCompletionReminder, trainer, client3);

            // Assert
            Assert.Contains("O'Brien", message1);
            Assert.Contains("José", message2);
            Assert.Contains("Anne-Marie", message3);
        }

        [Fact]
        public void TestGetWorkoutCollectionMessageHandlesEdgeCaseDaysOfMonth()
        {
            // Test days 11-19 which all use "th"
            var date11 = new DateTime(2025, 5, 11, 12, 0, 0);
            var date12 = new DateTime(2025, 5, 12, 12, 0, 0);
            var date13 = new DateTime(2025, 5, 13, 12, 0, 0);

            var message11 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date11);
            var message12 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date12);
            var message13 = NotificationMessageHelper.GetWorkoutCollectionMessage(1, date13);

            Assert.Contains("11th", message11);
            Assert.Contains("12th", message12);
            Assert.Contains("13th", message13);
        }
    }
}
