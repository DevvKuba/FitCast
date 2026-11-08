using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class BookingSeriesRepositoryTests
    {
        private readonly DataContext _context;
        private readonly BookingSeriesRepository _bookingSeriesRepository;
        private readonly UnitOfWork _unitOfWork;

        public BookingSeriesRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _bookingSeriesRepository = testUnitOfWork.bookingSeriesRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
        }

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john")
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }

        private async Task<Client> SeedClientAsync(Trainer trainer, string firstName)
        {
            var client = new Client { FirstName = firstName, Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return client;
        }

        private async Task<BookingSeries> SeedSeriesAsync(Trainer trainer, Client client, string title = "weekly session")
        {
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = title,
                Duration = 60,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                StartTime = new TimeOnly(18, 0),
                MaterializedUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90),
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            await _unitOfWork.Complete();
            return series;
        }

        private static BookingSeriesCreateDto CreateDto(int clientId, SessionRecurrence recurrence = SessionRecurrence.Weekly) => new()
        {
            ClientId = clientId,
            Title = "new series",
            Description = "notes",
            Duration = 45,
            StartDate = new DateOnly(2026, 10, 11),
            StartTime = new TimeOnly(9, 0),
            Recurrence = recurrence
        };

        [Fact]
        public async Task GetAllBookingSeriesAsync_ReturnsEverySeriesAcrossTrainers()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            var allSeries = await _bookingSeriesRepository.GetAllBookingSeriesAsync();

            Assert.Equal(2, allSeries.Count);
        }

        [Fact]
        public async Task GetAllBookingSeriesAsync_LoadsEachSeriesTrainer()
        {
            var trainer = await SeedTrainerAsync();
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            _context.ChangeTracker.Clear(); // otherwise EF fills in Trainer from the tracked seed, with or without the Include

            var allSeries = await _bookingSeriesRepository.GetAllBookingSeriesAsync();

            var series = Assert.Single(allSeries);
            Assert.NotNull(series.Trainer);
            Assert.Equal(trainer.Id, series.Trainer.Id);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesByIdAsync_ReturnsTheTrainersOwnSeries()
        {
            var trainer = await SeedTrainerAsync();
            var series = await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesByIdAsync(trainer.Id, series.Id);

            Assert.NotNull(result);
            Assert.Equal(series.Id, result.Id);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesByIdAsync_ReturnsNullForAnotherTrainersSeries()
        {
            // Query-scoped ownership: a valid id under someone else is indistinguishable from a missing one.
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesByIdAsync(trainer.Id, otherSeries.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesByIdAsync_ReturnsNullWhenSeriesDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesByIdAsync(trainer.Id, 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetBookingSeriesByIdWithTrainer_LoadsTheTrainerNavigation()
        {
            var trainer = await SeedTrainerAsync();
            var series = await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            // Clear tracking so the navigation can only come from the Include, not relationship fix-up.
            _context.ChangeTracker.Clear();

            var result = await _bookingSeriesRepository.GetBookingSeriesByIdWithTrainer(trainer.Id, series.Id);

            Assert.NotNull(result);
            Assert.NotNull(result.Trainer);
            Assert.Equal(trainer.Id, result.Trainer.Id);
        }

        [Fact]
        public async Task GetBookingSeriesByIdWithTrainer_ReturnsNullForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            var result = await _bookingSeriesRepository.GetBookingSeriesByIdWithTrainer(trainer.Id, otherSeries.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateBookingSeriesUnderTrainerAsync_MapsDtoAndAssignsTrainer()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");

            var series = await _bookingSeriesRepository.CreateBookingSeriesUnderTrainerAsync(trainer, CreateDto(client.Id));
            await _unitOfWork.Complete();

            var saved = await _context.BookingSeries.SingleAsync();
            Assert.Same(series, saved);
            Assert.Equal(trainer.Id, saved.TrainerId);
            Assert.Equal(client.Id, saved.ClientId);
            Assert.Equal("new series", saved.Title);
            Assert.Equal("notes", saved.Description);
            Assert.Equal(45, saved.Duration);
            Assert.Equal(new DateOnly(2026, 10, 11), saved.StartDate);
            Assert.Equal(new TimeOnly(9, 0), saved.StartTime);
            Assert.Equal(SessionRecurrence.Weekly, saved.Recurrence);
        }

        [Theory]
        [InlineData(SessionRecurrence.Weekly, 2026, 10, 4)]
        [InlineData(SessionRecurrence.BiWeekly, 2026, 9, 27)]
        public async Task CreateBookingSeriesUnderTrainerAsync_SeedsWatermarkOneIntervalBeforeStartDate(
            SessionRecurrence recurrence, int year, int month, int day)
        {
            // The seed is what makes the materialization loop's first step land exactly on StartDate.
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");

            var series = await _bookingSeriesRepository.CreateBookingSeriesUnderTrainerAsync(trainer, CreateDto(client.Id, recurrence));

            Assert.Equal(new DateOnly(year, month, day), series.MaterializedUntil);
        }

        [Fact]
        public async Task CreateBookingSeriesUnderTrainerAsync_OnlyStagesTheSeriesUntilComplete()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");

            var series = await _bookingSeriesRepository.CreateBookingSeriesUnderTrainerAsync(trainer, CreateDto(client.Id));

            Assert.Equal(EntityState.Added, _context.Entry(series).State);
            Assert.False(await _context.BookingSeries.AnyAsync());
        }

        [Fact]
        public async Task UpdateBookingSeriesAndResetMaterializedUntil_MapsEditableFields_AndKeepsClientAndRecurrence()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);
            var newStartDate = series.StartDate.AddDays(14);

            _bookingSeriesRepository.UpdateBookingSeriesAndResetMaterializedUntil(series, UpdateDto(newStartDate, new TimeOnly(10, 30), "evening session", "new notes", 45));
            await _unitOfWork.Complete();

            var saved = await _context.BookingSeries.SingleAsync();
            Assert.Equal(newStartDate, saved.StartDate);
            Assert.Equal(new TimeOnly(10, 30), saved.StartTime);
            Assert.Equal("evening session", saved.Title);
            Assert.Equal("new notes", saved.Description);
            Assert.Equal(45, saved.Duration);
            Assert.Equal(alice.Id, saved.ClientId);
            Assert.Equal(SessionRecurrence.Weekly, saved.Recurrence);
        }

        [Theory]
        [InlineData(SessionRecurrence.Weekly, 7)]
        [InlineData(SessionRecurrence.BiWeekly, 14)]
        public async Task UpdateBookingSeriesAndResetMaterializedUntil_ReseedsWatermarkOneIntervalBeforeTheNewStart(SessionRecurrence recurrence, int interval)
        {
            // Same seed rule as creation, so re-materialization's first step lands exactly on the new StartDate.
            var trainer = await SeedTrainerAsync();
            var series = await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            series.Recurrence = recurrence;
            var newStartDate = series.StartDate.AddDays(3);

            _bookingSeriesRepository.UpdateBookingSeriesAndResetMaterializedUntil(series, UpdateDto(newStartDate, new TimeOnly(10, 30)));

            Assert.Equal(newStartDate.AddDays(-interval), series.MaterializedUntil);
        }

        [Fact]
        public async Task DeleteBookingSeries_RemovesOnlyThatSeries()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer, "alice");
            var toDelete = await SeedSeriesAsync(trainer, client, "to delete");
            var toKeep = await SeedSeriesAsync(trainer, client, "to keep");

            _bookingSeriesRepository.DeleteBookingSeries(toDelete);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookingSeries.AnyAsync(b => b.Id == toDelete.Id));
            Assert.True(await _context.BookingSeries.AnyAsync(b => b.Id == toKeep.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForTrainer_RemovesEverySeriesAcrossTheirClients()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            await SeedSeriesAsync(trainer, alice);
            await SeedSeriesAsync(trainer, alice, "one-off");
            await SeedSeriesAsync(trainer, bob);

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookingSeries.AnyAsync(b => b.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForClient_RemovesOnlyThatClientsSeries()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var bob = await SeedClientAsync(trainer, "bob");
            await SeedSeriesAsync(trainer, alice);
            await SeedSeriesAsync(trainer, bob);

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(alice);
            await _unitOfWork.Complete();

            Assert.False(await _context.BookingSeries.AnyAsync(b => b.ClientId == alice.Id));
            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.ClientId == bob.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_ForTrainer_LeavesOtherTrainersSeriesUntouched()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));
            await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);
            await _unitOfWork.Complete();

            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.TrainerId == otherTrainer.Id));
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_StagesNothingWhenUserHasNoSeries()
        {
            var trainer = await SeedTrainerAsync();

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);

            Assert.False(_unitOfWork.HasChanges());
        }

        [Fact]
        public async Task RemoveAllBookingSeriesForUserAsync_OnlyStagesRemovalUntilComplete()
        {
            // Matches every other repository here: the caller decides when to persist.
            var trainer = await SeedTrainerAsync();
            await SeedSeriesAsync(trainer, await SeedClientAsync(trainer, "alice"));

            await _bookingSeriesRepository.RemoveAllBookingSeriesForUserAsync(trainer);

            Assert.True(_unitOfWork.HasChanges());
            Assert.Equal(1, await _context.BookingSeries.CountAsync(b => b.TrainerId == trainer.Id));
        }

        [Fact]
        public async Task GetTrainerBookingSeriesDtosAsync_ReturnsOnlyTheTrainersSeries_OrderedByClientThenTitle()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var bob = await SeedClientAsync(trainer, "bob");
            var alice = await SeedClientAsync(trainer, "alice");
            await SeedSeriesAsync(trainer, bob, "morning session");
            await SeedSeriesAsync(trainer, alice, "weekend session");
            await SeedSeriesAsync(trainer, alice, "evening session");
            await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "carl"), "other trainers session");

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesDtosAsync(trainer.Id);

            Assert.Equal(["alice", "alice", "bob"], result.Select(b => b.ClientName));
            Assert.Equal(["evening session", "weekend session", "morning session"], result.Select(b => b.Title));
        }

        [Fact]
        public async Task GetTrainerBookingSeriesDtosAsync_ReturnsEmptyListWhenTrainerHasNoSeries()
        {
            var trainer = await SeedTrainerAsync();

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesDtosAsync(trainer.Id);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesDtoByIdAsync_MapsEverySeriesField()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer, "alice");
            var series = await SeedSeriesAsync(trainer, alice);

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesDtoByIdAsync(trainer.Id, series.Id);

            Assert.NotNull(result);
            Assert.Equal(series.Id, result.Id);
            Assert.Equal(alice.Id, result.ClientId);
            Assert.Equal("alice", result.ClientName);
            Assert.Equal(series.Title, result.Title);
            Assert.Equal(series.Description, result.Description);
            Assert.Equal(series.Duration, result.Duration);
            Assert.Equal(series.StartDate, result.StartDate);
            Assert.Equal(series.StartTime, result.StartTime);
            Assert.Equal(series.Recurrence, result.Recurrence);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesDtoByIdAsync_ReturnsNullForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));

            var result = await _bookingSeriesRepository.GetTrainerBookingSeriesDtoByIdAsync(trainer.Id, otherSeries.Id);

            Assert.Null(result);
        }

        private static BookingSeriesUpdateDto UpdateDto(DateOnly startDate, TimeOnly startTime, string title = "weekly session", string? description = null, int duration = 60) => new()
        {
            Title = title,
            Description = description,
            Duration = duration,
            StartDate = startDate,
            StartTime = startTime
        };
    }
}
