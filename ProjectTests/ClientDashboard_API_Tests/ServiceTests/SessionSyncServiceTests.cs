using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    public class FakeClientBlockTerminationHelper : IClientBlockTerminationHelper
    {
        public int CallCount { get; private set; }
        public List<int> ProcessedClientIds { get; } = new();

        public Task<ApiResponseDto<string>> CreateAllAdequateEntityReminderAsync(Client client)
        {
            CallCount++;
            ProcessedClientIds.Add(client.Id);
            return Task.FromResult(new ApiResponseDto<string> { Data = "Success", Message = "Block termination processed", Success = true });
        }
    }

    // Captures the client handed over by the sync service, so a test can check its Trainer was loaded
    public class SpyClientBlockTerminationHelper : IClientBlockTerminationHelper
    {
        public int CallCount { get; private set; }
        public Client? CapturedClient { get; private set; }

        public Task<ApiResponseDto<string>> CreateAllAdequateEntityReminderAsync(Client client)
        {
            CallCount++;
            CapturedClient = client;
            return Task.FromResult(new ApiResponseDto<string> { Data = "Success", Message = "captured", Success = true });
        }
    }

    public class SessionSyncServiceTests
    {
        private static readonly DateTimeOffset PinnedNow = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly FakeClientBlockTerminationHelper _blockTerminator;
        private readonly SessionSyncService _sessionSyncService;

        public SessionSyncServiceTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _mapper = testUnitOfWork.mapper;
            _passwordHasher = testUnitOfWork.passwordHasher;
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;
            _blockTerminator = new FakeClientBlockTerminationHelper();
            _sessionSyncService = new SessionSyncService(_unitOfWork, _blockTerminator, new FixedTimeProvider(PinnedNow));
        }

        [Fact]
        public async Task SyncSessions_SyncOn_MarksFinishedSlotSyncedAndCreatesLinkedWorkout()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 2, totalBlockSessions: 8);
            var slot = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3), durationMinutes: 45, title: "Strength Session");

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(1, syncedCount);
            Assert.Equal(SessionStatus.Synced, (await ReloadSlotAsync(slot.Id)).Status);

            var workout = await _context.Workouts.SingleAsync();
            Assert.Equal(slot.Id, workout.BookedSessionSlotId);
            Assert.Equal(client.Id, workout.ClientId);
            Assert.Equal("Strength Session", workout.WorkoutTitle);
            Assert.Equal(45, workout.Duration);
            Assert.Equal(3, workout.CurrentBlockSession);
            Assert.Equal(3, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task SyncSessions_SyncOff_MarksFinishedSlotCompletedWithoutCounting()
        {
            var trainer = await SeedTrainerAsync(calendarSyncEnabled: false);
            var client = await SeedClientAsync(trainer, currentBlockSession: 2, totalBlockSessions: 8);
            var slot = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3));

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, syncedCount);
            Assert.Equal(SessionStatus.Completed, (await ReloadSlotAsync(slot.Id)).Status);
            Assert.Empty(await _context.Workouts.ToListAsync());
            Assert.Equal(2, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task SyncSessions_LeavesSlotsThatHaveNotFinishedScheduled()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var inProgress = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddMinutes(-30), durationMinutes: 60);
            var upcoming = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddDays(1));

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, syncedCount);
            Assert.Equal(SessionStatus.Scheduled, (await ReloadSlotAsync(inProgress.Id)).Status);
            Assert.Equal(SessionStatus.Scheduled, (await ReloadSlotAsync(upcoming.Id)).Status);
            Assert.Empty(await _context.Workouts.ToListAsync());
        }

        [Theory]
        [InlineData(SessionStatus.Completed)]
        [InlineData(SessionStatus.Cancelled)]
        [InlineData(SessionStatus.Synced)]
        public async Task SyncSessions_IgnoresSlotsThatAreNoLongerScheduled(SessionStatus status)
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var slot = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3), status: status);

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, syncedCount);
            Assert.Equal(status, (await ReloadSlotAsync(slot.Id)).Status);
            Assert.Empty(await _context.Workouts.ToListAsync());
        }

        [Fact]
        public async Task SyncSessions_RunningTwiceCountsEachSlotOnce()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 0, totalBlockSessions: 8);
            await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3));

            await _sessionSyncService.SyncSessionsAsync(trainer);
            var secondRunCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, secondRunCount);
            Assert.Single(await _context.Workouts.ToListAsync());
            Assert.Equal(1, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task SyncSessions_SyncsTwoSessionsOnTheSameDayForOneClient()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 0, totalBlockSessions: 8);
            await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-5));
            await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-2));

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(2, syncedCount);
            Assert.Equal(2, (await _context.Workouts.ToListAsync()).Count);
            Assert.Equal(2, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task SyncSessions_TriggersBlockTerminationWhenTheBlockCompletes()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 3, totalBlockSessions: 4);
            await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3));

            await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(1, _blockTerminator.CallCount);
            Assert.Equal(client.Id, _blockTerminator.ProcessedClientIds.Single());
        }

        [Fact]
        public async Task SyncSessions_DoesNotTriggerBlockTerminationMidBlock()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 1, totalBlockSessions: 4);
            await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-3));

            await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, _blockTerminator.CallCount);
        }

        [Fact]
        public async Task SyncSessions_ProcessesSlotsOldestFirstSoTheBlockWrapsCorrectly()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, currentBlockSession: 3, totalBlockSessions: 4);
            var later = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddHours(-2), title: "later");
            var earlier = await SeedSlotAsync(trainer, client, PinnedNow.UtcDateTime.AddDays(-1), title: "earlier");

            await _sessionSyncService.SyncSessionsAsync(trainer);

            var workouts = await _context.Workouts.ToListAsync();
            Assert.Equal(4, workouts.Single(w => w.BookedSessionSlotId == earlier.Id).CurrentBlockSession);
            Assert.Equal(1, workouts.Single(w => w.BookedSessionSlotId == later.Id).CurrentBlockSession);
            Assert.Equal(1, _blockTerminator.CallCount);
        }

        [Fact]
        public async Task SyncSessions_DatesTheWorkoutInTheTrainersTimeZone()
        {
            // 23:30 UTC on 9 Nov is already 10 Nov in Auckland
            var trainer = await SeedTrainerAsync(timeZoneId: SupportedTimeZones.Auckland);
            var client = await SeedClientAsync(trainer);
            await SeedSlotAsync(trainer, client, new DateTime(2026, 11, 9, 23, 30, 0, DateTimeKind.Utc));

            await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(new DateOnly(2026, 11, 10), (await _context.Workouts.SingleAsync()).SessionDate);
        }

        [Fact]
        public async Task SyncSessions_OnlyProcessesTheGivenTrainersSlots()
        {
            var trainer = await SeedTrainerAsync();
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherClient = await SeedClientAsync(otherTrainer);
            var otherSlot = await SeedSlotAsync(otherTrainer, otherClient, PinnedNow.UtcDateTime.AddHours(-3));

            var syncedCount = await _sessionSyncService.SyncSessionsAsync(trainer);

            Assert.Equal(0, syncedCount);
            Assert.Equal(SessionStatus.Scheduled, (await ReloadSlotAsync(otherSlot.Id)).Status);
        }

        // Mirrors the job: the trainer is loaded in one scope and synced in another, so the client
        // must be loaded with its Trainer for the block reminders and auto-payment to run
        [Fact]
        public async Task SyncSessions_SuppliesClientWithTrainerWhenTrainerLoadedInSeparateScope()
        {
            var databaseName = Guid.NewGuid().ToString();
            int trainerId;

            using (var seedContext = CreateContext(databaseName))
            {
                var seedTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoPaymentSetting = true };
                seedContext.Trainer.Add(seedTrainer);
                await seedContext.SaveChangesAsync();

                var seedClient = new Client { FirstName = "nathan", Role = UserRole.Client, TrainerId = seedTrainer.Id, CurrentBlockSession = 3, TotalBlockSessions = 4 };
                seedContext.Client.Add(seedClient);
                await seedContext.SaveChangesAsync();

                seedContext.BookedSessionsSlot.Add(NewSlot(seedTrainer.Id, seedClient.Id, PinnedNow.UtcDateTime.AddHours(-3), 60, "Final Session", SessionStatus.Scheduled));
                await seedContext.SaveChangesAsync();

                trainerId = seedTrainer.Id;
            }

            Trainer detachedTrainer;
            using (var scopeAContext = CreateContext(databaseName))
            {
                detachedTrainer = (await new TrainerRepository(scopeAContext, _mapper).GetTrainerByIdAsync(trainerId))!;
            }

            using var scopeBContext = CreateContext(databaseName);
            var spyTerminator = new SpyClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(CreateUnitOfWork(scopeBContext), spyTerminator, new FixedTimeProvider(PinnedNow));

            await sessionSyncService.SyncSessionsAsync(detachedTrainer);

            Assert.Equal(1, spyTerminator.CallCount);
            Assert.NotNull(spyTerminator.CapturedClient!.Trainer);
        }

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john", bool calendarSyncEnabled = true, string timeZoneId = SupportedTimeZones.London)
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer, CalendarSyncEnabled = calendarSyncEnabled, TimeZoneId = timeZoneId };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }

        private async Task<Client> SeedClientAsync(Trainer trainer, int currentBlockSession = 1, int? totalBlockSessions = 8)
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = currentBlockSession, TotalBlockSessions = totalBlockSessions };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return client;
        }

        private async Task<BookedSessionSlot> SeedSlotAsync(Trainer trainer, Client client, DateTime startUtc, int durationMinutes = 60,
            string title = "session", SessionStatus status = SessionStatus.Scheduled)
        {
            var slot = NewSlot(trainer.Id, client.Id, startUtc, durationMinutes, title, status);
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();
            return slot;
        }

        private static BookedSessionSlot NewSlot(int trainerId, int clientId, DateTime startUtc, int durationMinutes, string title, SessionStatus status) => new()
        {
            TrainerId = trainerId,
            ClientId = clientId,
            Title = title,
            StartDateTime = startUtc,
            EndDateTime = startUtc.AddMinutes(durationMinutes),
            Status = status
        };

        private async Task<BookedSessionSlot> ReloadSlotAsync(int slotId) =>
            await _context.BookedSessionsSlot.AsNoTracking().SingleAsync(s => s.Id == slotId);

        private static DataContext CreateContext(string databaseName)
        {
            return new DataContext(new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);
        }

        private UnitOfWork CreateUnitOfWork(DataContext context)
        {
            var userRepository = new UserRepository(context, _passwordHasher);
            var clientRepository = new ClientRepository(context, _passwordHasher, _mapper);
            var workoutRepository = new WorkoutRepository(context);
            var trainerRepository = new TrainerRepository(context, _mapper);
            var notificationRepository = new NotificationRepository(context);
            var paymentRepository = new PaymentRepository(context, _mapper);
            var emailVerificationTokenRepository = new EmailVerificationTokenRepository(context);
            var passwordResetTokenRepository = new PasswordResetTokenRepository(context);
            var clientDailyFeatureRepository = new ClientDailyFeatureRepository(context);
            var trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(context, _mapper);
            var bookingSeriesRepository = new BookingSeriesRepository(context, _mapper);
            var bookedSessionSlotRepository = new BookedSessionSlotRepository(context, _mapper);

            return new UnitOfWork(context, userRepository, clientRepository, workoutRepository, trainerRepository, notificationRepository, new NotificationRecipientStatusRepository(context), paymentRepository, emailVerificationTokenRepository, clientDailyFeatureRepository, trainerDailyRevenueRepository, passwordResetTokenRepository, bookingSeriesRepository, bookedSessionSlotRepository);
        }
    }
}
