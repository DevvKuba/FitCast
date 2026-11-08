using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Jobs;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientDashboard_API_Tests.JobTests
{
    public class DailyMaterializationJobTests
    {
        // Clock is pinned so the 90 day window, and so the expected slot counts, don't depend on when the tests run
        private static readonly DateTimeOffset PinnedNow = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateOnly SeriesStartDate = new(2026, 11, 5);

        // Weekly from the day after PinnedNow: days 1, 8, ... 85 of the window
        private const int ExpectedSlotsInWindow = 13;

        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly DailyMaterializationJob _job;

        public DailyMaterializationJobTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            var materializationService = new BookingMaterializationService(_unitOfWork, new FixedTimeProvider(PinnedNow));
            _job = new DailyMaterializationJob(_unitOfWork, materializationService, NullLogger<DailyMaterializationJob>.Instance);
        }

        [Fact]
        public async Task Execute_SavesNewSlotsAndAdvancedWatermark()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var series = await SeedSeriesAsync(trainer, client, SeededWatermark());

            await RunJobAsync();

            Assert.Equal(ExpectedSlotsInWindow, await _context.BookedSessionsSlot.CountAsync(s => s.BookingSeriesId == series.Id));
            Assert.Equal(SeriesStartDate.AddDays(84), await SavedWatermarkAsync(series.Id));
        }

        [Fact]
        public async Task Execute_RunningTwiceCreatesNoDuplicates()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            await SeedSeriesAsync(trainer, client, SeededWatermark());

            await RunJobAsync();
            await RunJobAsync();

            Assert.Equal(ExpectedSlotsInWindow, await _context.BookedSessionsSlot.CountAsync());
        }

        [Fact]
        public async Task Execute_ContinuesPastASeriesThatFails()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var misalignedWatermark = SeriesStartDate.AddDays(3);
            var failingSeries = await SeedSeriesAsync(trainer, client, misalignedWatermark);
            var validSeries = await SeedSeriesAsync(trainer, client, SeededWatermark());

            await RunJobAsync();

            Assert.Equal(0, await _context.BookedSessionsSlot.CountAsync(s => s.BookingSeriesId == failingSeries.Id));
            Assert.Equal(misalignedWatermark, await SavedWatermarkAsync(failingSeries.Id));
            Assert.Equal(ExpectedSlotsInWindow, await _context.BookedSessionsSlot.CountAsync(s => s.BookingSeriesId == validSeries.Id));
        }

        [Fact]
        public async Task Execute_WithNoSeries_FinishesWithoutCreatingSlots()
        {
            var exception = await Record.ExceptionAsync(RunJobAsync);

            Assert.Null(exception);
            Assert.Equal(0, await _context.BookedSessionsSlot.CountAsync());
        }

        // Each Quartz run gets a fresh scope, so tracked entities are cleared to load everything the way a real run does.
        // The job never reads its IJobExecutionContext, so no context is supplied.
        private async Task RunJobAsync()
        {
            _context.ChangeTracker.Clear();
            await _job.Execute(null!);
        }

        // Same seed the repository gives a new series: one interval before the start date
        private static DateOnly SeededWatermark() => SeriesStartDate.AddDays(-7);

        private async Task<DateOnly> SavedWatermarkAsync(int bookingSeriesId)
        {
            var series = await _context.BookingSeries.AsNoTracking().SingleAsync(s => s.Id == bookingSeriesId);
            return series.MaterializedUntil;
        }

        private async Task<(Trainer Trainer, Client Client)> SeedTrainerAndClientAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, TimeZoneId = SupportedTimeZones.London };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            return (trainer, client);
        }

        private async Task<BookingSeries> SeedSeriesAsync(Trainer trainer, Client client, DateOnly materializedUntil)
        {
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = SeriesStartDate,
                StartTime = new TimeOnly(9, 0),
                MaterializedUntil = materializedUntil,
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            await _unitOfWork.Complete();
            return series;
        }
    }
}
