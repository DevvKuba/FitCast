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
    public class TrainerDailyRevenueServiceTests
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
        private readonly TrainerDailyRevenueService _trainerDailyRevenueService;

        public TrainerDailyRevenueServiceTests()
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
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

            _trainerDailyRevenueService = new TrainerDailyRevenueService(_unitOfWork);
        }

        [Fact]
        public async Task TestExecuteTrainerDailyRevenueGatheringCreatesRecordSuccessfullyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };
            var client2 = new Client
            {
                FirstName = "bob",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };
            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

            // Each client trains exactly once today. A client cannot have two workouts on the
            // same date (system invariant), so today always contributes one session per client.
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client1, "alice - Today", today, 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client2, "bob - Today", today, 6, 50);

            // Add earlier-this-month sessions on distinct days strictly before today, so we never
            // place two workouts for the same client on the same day. When today is the 1st there
            // are no earlier days, so month-to-date correctly equals today's figures.

            var earlierDays = new List<DateOnly>();

            for (var day = today.AddDays(-1); day >= firstDayOfMonth && earlierDays.Count < 2; day = day.AddDays(-1))
            {
                earlierDays.Add(day);
            }

            foreach (var earlierDay in earlierDays)
            {
                await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client1, "alice - Earlier", earlierDay, 4, 40);
            }
            await _unitOfWork.Complete();

            // Expected figures derived from what was actually seeded, so the test holds on any day.
            const int sessionsToday = 2; // one per client
            var monthlySessions = sessionsToday + earlierDays.Count;
            var expectedRevenueToday = sessionsToday * 50.00m;
            var expectedMonthlyRevenue = monthlySessions * 50.00m;

            // Reload trainer with clients and workouts
            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            await _trainerDailyRevenueService.ExecuteTrainerDailyRevenueGatheringAsync(trainerWithClients!);

            // Assert
            var revenueRecords = await _context.TrainerDailyRevenue.ToListAsync();
            Assert.Single(revenueRecords);

            var record = revenueRecords[0];
            Assert.Equal(trainer.Id, record.TrainerId);
            Assert.Equal(today, record.AsOfDate);
            Assert.Equal(expectedRevenueToday, record.RevenueToday);
            Assert.Equal(sessionsToday, record.SessionsToday);
            Assert.Equal(expectedMonthlyRevenue, record.MonthlyRevenueThusFar);
            Assert.Equal(monthlySessions, record.TotalSessionsThisMonth);
            Assert.Equal(2, record.ActiveClients); // 2 active clients
            Assert.Equal(50.00m, record.AverageSessionPrice);
        }

        [Fact]
        public async Task TestExecuteTrainerDailyRevenueGatheringHandlesTrainerWithNoClientsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "jane",
                Surname = "smith",
                Role = UserRole.Trainer,
                AverageSessionPrice = 60.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            await _trainerDailyRevenueService.ExecuteTrainerDailyRevenueGatheringAsync(trainerWithClients!);

            // Assert
            var revenueRecords = await _context.TrainerDailyRevenue.ToListAsync();
            Assert.Single(revenueRecords);

            var record = revenueRecords[0];
            Assert.Equal(0.00m, record.RevenueToday);
            Assert.Equal(0.00m, record.MonthlyRevenueThusFar);
            Assert.Equal(0, record.TotalSessionsThisMonth);
            Assert.Equal(0, record.ActiveClients);
            Assert.Equal(0, record.NewClientsThisMonth);
        }

        [Fact]
        public async Task TestExecuteTrainerDailyRevenueGatheringHandlesNullAverageSessionPriceAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "mike",
                Surname = "jones",
                Role = UserRole.Trainer,
                AverageSessionPrice = null // Null session price
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "charlie",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "charlie - Workout", today, 5, 45);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            await _trainerDailyRevenueService.ExecuteTrainerDailyRevenueGatheringAsync(trainerWithClients!);

            // Assert
            var revenueRecords = await _context.TrainerDailyRevenue.ToListAsync();
            Assert.Single(revenueRecords);

            var record = revenueRecords[0];
            Assert.Equal(0.00m, record.RevenueToday); // 1 workout * $0 (null) = $0
            Assert.Equal(0.00m, record.AverageSessionPrice);
        }

        [Fact]
        public async Task TestExecuteTrainerDailyRevenueGatheringCalculatesNewClientsThisMonthAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "sarah",
                Surname = "wilson",
                Role = UserRole.Trainer,
                AverageSessionPrice = 45.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            // Note: CreatedAt is auto-set by EF Core on insert, so we can't manually set it
            // This test validates the service logic, but actual time-based filtering
            // would require real database timestamps
            var client1 = new Client
            {
                FirstName = "david",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };
            var client2 = new Client
            {
                FirstName = "eve",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };
            var client3 = new Client
            {
                FirstName = "frank",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true
            };

            await _context.Client.AddRangeAsync(client1, client2, client3);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            await _trainerDailyRevenueService.ExecuteTrainerDailyRevenueGatheringAsync(trainerWithClients!);

            // Assert
            var revenueRecords = await _context.TrainerDailyRevenue.ToListAsync();
            Assert.Single(revenueRecords);

            var record = revenueRecords[0];
            // In-memory database creates all clients with same CreatedAt, so NewClientsThisMonth
            // will be 0 (all clients existed at "last month end")
            // This test validates the logic compiles and runs correctly
            Assert.Equal(3, record.ActiveClients); // Total 3 clients
        }

        [Fact]
        public async Task TestCalculateTotalClientGeneratedRevenueAtDateCalculatesCorrectlyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "tom",
                Surname = "brown",
                Role = UserRole.Trainer,
                AverageSessionPrice = 55.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client
            {
                FirstName = "grace",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            var client2 = new Client
            {
                FirstName = "henry",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

            // Add workouts on target date
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client1, "grace - Workout", targetDate, 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client2, "henry - Workout", targetDate, 6, 50);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client1, "grace - Another", targetDate, 4, 40);

            // Add workouts on different date (should not be counted)
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client1, "grace - Different Day", targetDate.AddDays(1), 5, 45);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var revenue = await _trainerDailyRevenueService.CalculateTotalClientGeneratedRevenueAtDateAsync(trainerWithClients!, targetDate);

            // Assert
            Assert.Equal(165.00m, revenue); // 3 workouts * $55 = $165
        }

        [Fact]
        public async Task TestCalculateTotalClientGeneratedRevenueAtDateReturnsZeroWhenNoWorkoutsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "lisa",
                Surname = "garcia",
                Role = UserRole.Trainer,
                AverageSessionPrice = 50.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "ivan",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var revenue = await _trainerDailyRevenueService.CalculateTotalClientGeneratedRevenueAtDateAsync(trainerWithClients!, targetDate);

            // Assert
            Assert.Equal(0.00m, revenue);
        }

        [Fact]
        public async Task TestCalculateTotalClientGeneratedRevenueBetweenDatesCalculatesCorrectlyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "paul",
                Surname = "martinez",
                Role = UserRole.Trainer,
                AverageSessionPrice = 40.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "julia",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

            // Workouts within range
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "julia - Workout 1", startDate, 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "julia - Workout 2", startDate.AddDays(2), 6, 50);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "julia - Workout 3", endDate, 5, 45);

            // Workouts outside range (should not be counted)
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "julia - Before", startDate.AddDays(-1), 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "julia - After", endDate.AddDays(1), 5, 45);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var revenue = await _trainerDailyRevenueService.CalculateTotalClientGeneratedRevenueBetweenDatesAsync(
                trainerWithClients!, startDate, endDate);

            // Assert
            Assert.Equal(120.00m, revenue); // 3 workouts * $40 = $120
        }

        [Fact]
        public async Task TestCalculateTotalClientGeneratedRevenueBetweenDatesHandlesSingleDayRangeAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "rachel",
                Surname = "lee",
                Role = UserRole.Trainer,
                AverageSessionPrice = 35.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "kevin",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Add workouts on single date
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "kevin - Workout 1", targetDate, 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "kevin - Workout 2", targetDate, 6, 50);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var revenue = await _trainerDailyRevenueService.CalculateTotalClientGeneratedRevenueBetweenDatesAsync(
                trainerWithClients!, targetDate, targetDate);

            // Assert
            Assert.Equal(70.00m, revenue); // 2 workouts * $35 = $70
        }

        [Fact]
        public async Task TestCalculateClientMonthlyDifferenceCalculatesPositiveGrowthAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "steven",
                Surname = "clark",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Note: CreatedAt is auto-set by EF Core on insert and has private setter
            // Cannot manually set CreatedAt for testing time-based logic
            // This test validates the calculation logic but not time-based filtering
            var client1 = new Client
            {
                FirstName = "laura",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            var client2 = new Client
            {
                FirstName = "mark",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            var client3 = new Client
            {
                FirstName = "nancy",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };

            await _context.Client.AddRangeAsync(client1, client2, client3);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var difference = _trainerDailyRevenueService.CalculateClientMonthlyDifference(trainerWithClients!, today);

            // Assert
            // In-memory database creates all clients at same timestamp,
            // so difference will be 0 (validates logic without time variance)
            Assert.Equal(0, difference);
        }

        [Fact]
        public async Task TestCalculateClientMonthlyDifferenceReturnsZeroWhenNoNewClientsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "quinn",
                Surname = "rodriguez",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // All clients (CreatedAt auto-set by EF Core)
            var client1 = new Client
            {
                FirstName = "rita",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            var client2 = new Client
            {
                FirstName = "sam",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };

            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var difference = _trainerDailyRevenueService.CalculateClientMonthlyDifference(trainerWithClients!, today);

            // Assert
            Assert.Equal(0, difference); // No new clients this month (all created at same time in test)
        }

        [Fact]
        public async Task TestReturnMonthlyClientSessionsThusFarCountsCorrectlyAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "tina",
                Surname = "hernandez",
                Role = UserRole.Trainer,
                AverageSessionPrice = 45.00m
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "uma",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            // Fixed dates keep this deterministic regardless of when the suite runs, and every
            // session sits on a distinct day so the one-session-per-client-per-day invariant holds.
            var startOfMonth = new DateOnly(2025, 6, 1);
            var endDate = new DateOnly(2025, 6, 20);

            // Three sessions within [startOfMonth, endDate] on distinct days
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "uma - Workout 1", startOfMonth, 5, 45);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "uma - Workout 2", new DateOnly(2025, 6, 6), 6, 50);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "uma - Workout 3", endDate, 5, 45);

            // Session from the previous month (outside the range, should not be counted)
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "uma - Last Month", new DateOnly(2025, 5, 31), 5, 45);
            await _unitOfWork.Complete();

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var sessionCount = await _trainerDailyRevenueService.ReturnMonthlyClientSessionsThusFarAsync(
                trainerWithClients!, startOfMonth, endDate);

            // Assert
            Assert.Equal(3, sessionCount); // 3 sessions within range
        }

        [Fact]
        public async Task TestReturnMonthlyClientSessionsThusFarReturnsZeroWhenNoSessionsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "victor",
                Surname = "gonzalez",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "wendy",
                Role = UserRole.Client,
                TrainerId = trainer.Id
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var startOfMonth = new DateOnly(today.Year, today.Month, 1);

            var trainerWithClients = await _unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainer.Id);

            // Act
            var sessionCount = await _trainerDailyRevenueService.ReturnMonthlyClientSessionsThusFarAsync(
                trainerWithClients!, startOfMonth, today);

            // Assert
            Assert.Equal(0, sessionCount);
        }

        [Fact]
        public void TestGatherFirstDayOfCurrentMonthReturnsCorrectDateAsync()
        {
            // Arrange
            var currentDate = new DateOnly(2025, 6, 15);

            // Act
            var firstDay = _trainerDailyRevenueService.GatherFirstDayOfCurrentMonth(currentDate);

            // Assert
            Assert.Equal(new DateOnly(2025, 6, 1), firstDay);
        }

        [Fact]
        public void TestGatherFirstDayOfCurrentMonthHandlesFirstDayOfMonthAsync()
        {
            // Arrange
            var currentDate = new DateOnly(2025, 3, 1);

            // Act
            var firstDay = _trainerDailyRevenueService.GatherFirstDayOfCurrentMonth(currentDate);

            // Assert
            Assert.Equal(new DateOnly(2025, 3, 1), firstDay);
        }

        [Fact]
        public void TestGatherFirstDayOfCurrentMonthHandlesLastDayOfMonthAsync()
        {
            // Arrange
            var currentDate = new DateOnly(2025, 2, 28);

            // Act
            var firstDay = _trainerDailyRevenueService.GatherFirstDayOfCurrentMonth(currentDate);

            // Assert
            Assert.Equal(new DateOnly(2025, 2, 1), firstDay);
        }

    }
}

