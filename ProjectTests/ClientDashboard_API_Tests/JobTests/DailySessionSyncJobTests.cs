using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using FakeNotificationService = ClientDashboard_API_Tests.ServiceTests.FakeNotificationService;

namespace ClientDashboard_API_Tests.JobTests
{
    public class FakeSessionSyncService : ISessionSyncService
    {
        public Dictionary<int, int> SyncedCountByTrainerId { get; } = new();
        public HashSet<int> FailingTrainerIds { get; } = new();
        public List<int> SyncedTrainerIds { get; } = new();

        public Task<int> SyncSessionsAsync(Trainer trainer)
        {
            SyncedTrainerIds.Add(trainer.Id);

            if (FailingTrainerIds.Contains(trainer.Id))
            {
                throw new InvalidOperationException("sync failed");
            }
            return Task.FromResult(SyncedCountByTrainerId.GetValueOrDefault(trainer.Id));
        }
    }

    public class DailySessionSyncJobTests
    {
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly FakeSessionSyncService _fakeSyncService;
        private readonly FakeNotificationService _fakeNotificationService;
        private readonly DailySessionSyncJob _job;

        public DailySessionSyncJobTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;
            _fakeSyncService = new FakeSessionSyncService();
            _fakeNotificationService = new FakeNotificationService();

            var services = new ServiceCollection();
            services.AddSingleton<IUnitOfWork>(_unitOfWork);
            services.AddSingleton<ISessionSyncService>(_fakeSyncService);
            services.AddSingleton<INotificationService>(_fakeNotificationService);
            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            _job = new DailySessionSyncJob(scopeFactory, NullLogger<DailySessionSyncJob>.Instance);
        }

        [Fact]
        public async Task Execute_SyncsEveryTrainerWhateverTheirCalendarSyncSetting()
        {
            var syncOn = await SeedTrainerAsync("john", calendarSyncEnabled: true);
            var syncOff = await SeedTrainerAsync("jane", calendarSyncEnabled: false);

            await _job.Execute(null!);

            Assert.Equal([syncOn.Id, syncOff.Id], _fakeSyncService.SyncedTrainerIds.Order().ToList());
        }

        [Fact]
        public async Task Execute_SendsTheSummaryOnlyToTrainersWithSyncedSessions()
        {
            var withSessions = await SeedTrainerAsync("john");
            await SeedTrainerAsync("jane");
            _fakeSyncService.SyncedCountByTrainerId[withSessions.Id] = 3;

            await _job.Execute(null!);

            var notification = Assert.Single(_fakeNotificationService.SentNotifications);
            Assert.Equal(withSessions.Id, notification.TrainerId);
            Assert.Equal("DailySessionSync", notification.Type);
        }

        [Fact]
        public async Task Execute_KeepsGoingWhenOneTrainersSyncFails()
        {
            var failing = await SeedTrainerAsync("john");
            var healthy = await SeedTrainerAsync("jane");
            _fakeSyncService.FailingTrainerIds.Add(failing.Id);
            _fakeSyncService.SyncedCountByTrainerId[healthy.Id] = 2;

            await _job.Execute(null!);

            Assert.Contains(healthy.Id, _fakeSyncService.SyncedTrainerIds);
            Assert.Equal(healthy.Id, Assert.Single(_fakeNotificationService.SentNotifications).TrainerId);
        }

        [Fact]
        public async Task Execute_WithNoTrainers_FinishesWithoutSyncing()
        {
            var exception = await Record.ExceptionAsync(() => _job.Execute(null!));

            Assert.Null(exception);
            Assert.Empty(_fakeSyncService.SyncedTrainerIds);
        }

        private async Task<Trainer> SeedTrainerAsync(string firstName, bool calendarSyncEnabled = true)
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer, CalendarSyncEnabled = calendarSyncEnabled };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }
    }
}
