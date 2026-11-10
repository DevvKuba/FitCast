using ClientDashboard_API.Authorization;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class BookingSeriesControllerTests
    {
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly BookingSeriesController _bookingSeriesController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public BookingSeriesControllerTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            var (authorizationService, currentUserAccessor, httpContextAccessor) =
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _bookingSeriesController = new BookingSeriesController(
                _unitOfWork,
                testUnitOfWork.mapper,
                currentUserAccessor,
                authorizationService,
                new BookingMaterializationService(_unitOfWork, TimeProvider.System));
            TestAuthHelpers.AttachHttpContext(_bookingSeriesController, _httpContextAccessor);
        }

        [Fact]
        public async Task CreateBookingSeriesAsync_SavesSeriesAndItsMaterializedSlots()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(client.Id));
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            var series = await _context.BookingSeries.SingleAsync();
            Assert.Equal(trainer.Id, series.TrainerId);
            Assert.Equal(13, await _context.BookedSessionsSlot.CountAsync(s => s.BookingSeriesId == series.Id));
            Assert.Equal(series.StartDate.AddDays(84), series.MaterializedUntil);
        }

        [Fact]
        public async Task CreateBookingSeriesAsync_ReturnsNotFoundWhenClientDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(999));
            var response = (result.Result as NotFoundObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.False(await _context.BookingSeries.AnyAsync());
        }

        [Fact]
        public async Task CreateBookingSeriesAsync_ReturnsForbiddenForAnotherTrainersClient()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherClient = await SeedClientAsync(otherTrainer, "bob");
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(otherClient.Id));
            var forbiddenResult = result.Result as ObjectResult;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult!.StatusCode);
            Assert.False(await _context.BookingSeries.AnyAsync());
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task CreateBookingSeriesAsync_ReturnsNotFoundWhenTrainerDoesNotExist()
        {
            AuthenticateAsTrainer(999);

            var result = await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(1));

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_UpdatesTheStartDate()
        {
            var trainer = await SeedTrainerAsync();
            var series = await SeedSeriesAsync(trainer, await SeedClientAsync(trainer));
            var newStartDate = series.StartDate.AddDays(14);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(newStartDate, new TimeOnly(9, 0)));
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(newStartDate, (await _context.BookingSeries.SingleAsync()).StartDate);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_ReturnsNotFoundForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            var originalStartDate = otherSeries.StartDate;
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.UpdateBookingSeriesAsync(otherSeries.Id, UpdateDto(originalStartDate.AddDays(14), new TimeOnly(9, 0)));

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(originalStartDate, (await _context.BookingSeries.SingleAsync()).StartDate);
        }

        [Fact]
        public async Task DeleteBookingSeriesAsync_RemovesTheSeriesAndItsUpcomingSlots()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            AuthenticateAsTrainer(trainer.Id);
            await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(client.Id));
            var series = await _context.BookingSeries.SingleAsync();

            var result = await _bookingSeriesController.DeleteBookingSeriesAsync(series.Id);
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.True(response.Data);
            Assert.False(await _context.BookingSeries.AnyAsync());
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task DeleteBookingSeriesAsync_KeepsPastSlotsAsOneOffSessions()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var series = await CreateSeriesThroughEndpointAsync(trainer, client);
            var pastSlot = await AddSeriesSlotAsync(series, DateTime.UtcNow.AddDays(-7));

            await _bookingSeriesController.DeleteBookingSeriesAsync(series.Id);

            // SetNull on the relationship: the past slot survives, unlinked from the deleted series
            var remainingSlot = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(pastSlot.Id, remainingSlot.Id);
            Assert.Null(remainingSlot.BookingSeriesId);
            Assert.False(await _context.BookingSeries.AnyAsync());
        }

        [Fact]
        public async Task DeleteBookingSeriesAsync_KeepsUpcomingSlotsThatAreNoLongerScheduled()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var series = await CreateSeriesThroughEndpointAsync(trainer, client);
            var completedSlot = await AddSeriesSlotAsync(series, DateTime.UtcNow.AddDays(2), SessionStatus.Completed);

            await _bookingSeriesController.DeleteBookingSeriesAsync(series.Id);

            var remainingSlot = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(completedSlot.Id, remainingSlot.Id);
            Assert.Null(remainingSlot.BookingSeriesId);
        }

        [Fact]
        public async Task DeleteBookingSeriesAsync_ReturnsNotFoundForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.DeleteBookingSeriesAsync(otherSeries.Id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.True(await _context.BookingSeries.AnyAsync(b => b.Id == otherSeries.Id));
        }

        // ---- Start date / time change: what happens to the series' existing slots ----

        [Fact]
        public async Task UpdateBookingSeriesAsync_SlotsKeepTheSeriesCadenceFromTheNewStartDate()
        {
            // Moving a weekly series 3 days later should give slots on newStart, newStart+7, newStart+14...
            // not every slot collapsed onto the same new date.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));
            var newStartDate = series.StartDate.AddDays(3);

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(newStartDate, new TimeOnly(9, 0)));

            var localDates = (await SeriesSlotsAsync(series))
                .Select(s => DateOnly.FromDateTime(ToLocal(s.StartDateTime, trainer.TimeZoneId)))
                .Where(date => date >= newStartDate)
                .ToList();
            Assert.NotEmpty(localDates);
            Assert.Equal(localDates.Count, localDates.Distinct().Count());
            Assert.Equal(newStartDate, localDates[0]);
            Assert.All(localDates.Zip(localDates.Skip(1)), pair => Assert.Equal(7, pair.Second.DayNumber - pair.First.DayNumber));
        }

        [Theory]
        [InlineData(SupportedTimeZones.London)]
        [InlineData(SupportedTimeZones.NewYork)]
        [InlineData(SupportedTimeZones.Sydney)]
        public async Task UpdateBookingSeriesAsync_SlotsReadTheNewStartTimeInTheTrainersZone(string timeZoneId)
        {
            // The new local time must be converted with the trainer's TimeZoneId on each slot's own date -
            // not the server's zone (UTC on Azure), and not one offset applied to every date.
            var trainer = await SeedTrainerAsync(timeZoneId: timeZoneId);
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate, new TimeOnly(10, 30)));

            var slots = await SeriesSlotsAsync(series);
            Assert.NotEmpty(slots);
            Assert.All(slots, slot => Assert.Equal(new TimeOnly(10, 30), TimeOnly.FromDateTime(ToLocal(slot.StartDateTime, timeZoneId))));
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_KeepsEachSlotsDuration()
        {
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate.AddDays(7), new TimeOnly(18, 0)));

            Assert.All(await SeriesSlotsAsync(series), slot => Assert.Equal(TimeSpan.FromMinutes(series.Duration), slot.EndDateTime - slot.StartDateTime));
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_LeavesPastSlotsUntouched()
        {
            // A session that already happened stays where it happened.
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var series = await CreateSeriesThroughEndpointAsync(trainer, client);
            var pastStart = DateTime.UtcNow.Date.AddDays(-7).AddHours(8);
            var pastSlot = new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                BookingSeriesId = series.Id,
                Title = series.Title,
                StartDateTime = pastStart,
                EndDateTime = pastStart.AddHours(1),
                Status = SessionStatus.Completed
            };
            await _context.BookedSessionsSlot.AddAsync(pastSlot);
            await _unitOfWork.Complete();

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate.AddDays(3), new TimeOnly(18, 0)));

            var saved = await _context.BookedSessionsSlot.SingleAsync(s => s.Id == pastSlot.Id);
            Assert.Equal(pastStart, saved.StartDateTime);
            Assert.Equal(pastStart.AddHours(1), saved.EndDateTime);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_LeavesOtherSeriesAndStandaloneSlotsUntouched()
        {
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var series = await CreateSeriesThroughEndpointAsync(trainer, client);

            await _bookingSeriesController.CreateBookingSeriesAsync(CreateDto(client.Id));
            var otherSeries = await _context.BookingSeries.SingleAsync(b => b.Id != series.Id);
            var standaloneStart = DateTime.UtcNow.Date.AddDays(2).AddHours(12);
            await _context.BookedSessionsSlot.AddAsync(new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "ad-hoc session",
                StartDateTime = standaloneStart,
                EndDateTime = standaloneStart.AddHours(1),
                Status = SessionStatus.Scheduled
            });
            await _unitOfWork.Complete();
            var otherSeriesStartsBefore = (await SeriesSlotsAsync(otherSeries)).Select(s => s.StartDateTime).ToList();

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate.AddDays(3), new TimeOnly(18, 0)));

            Assert.Equal(otherSeriesStartsBefore, (await SeriesSlotsAsync(otherSeries)).Select(s => s.StartDateTime));
            Assert.Equal(standaloneStart, (await _context.BookedSessionsSlot.SingleAsync(s => s.BookingSeriesId == null)).StartDateTime);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_SavesTheNewStartDateAndTimeOnTheSeries()
        {
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));
            var newStartDate = series.StartDate.AddDays(3);

            var result = await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(newStartDate, new TimeOnly(18, 0)));

            Assert.IsType<OkObjectResult>(result.Result);
            var saved = await _context.BookingSeries.SingleAsync();
            Assert.Equal(newStartDate, saved.StartDate);
            Assert.Equal(new TimeOnly(18, 0), saved.StartTime);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_SwitchesToTheNewScheduleFromTheNewStartDate()
        {
            // The change takes effect from the new start date: sessions before it stay on the old schedule,
            // and from it onwards none of the old schedule's sessions survive.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));
            var oldStartDate = series.StartDate;
            var newStartDate = oldStartDate.AddDays(3);

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(newStartDate, new TimeOnly(9, 0)));

            var localDates = (await SeriesSlotsAsync(series)).Select(s => DateOnly.FromDateTime(ToLocal(s.StartDateTime, trainer.TimeZoneId))).ToList();
            Assert.Equal([oldStartDate], localDates.Where(date => date < newStartDate));
            Assert.DoesNotContain(localDates.Where(date => date >= newStartDate), date => date.DayOfWeek == oldStartDate.DayOfWeek);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_RefillsTheWindowFromTheNewStartDate()
        {
            // Every occurrence from the new start up to today + 90 exists, and the watermark sits on the last one.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));
            var newStartDate = series.StartDate.AddDays(3);
            var windowEnd = TodayIn(trainer.TimeZoneId).AddDays(90);
            var expectedDates = Enumerable.Range(0, 20)
                .Select(week => newStartDate.AddDays(7 * week))
                .Where(date => date <= windowEnd)
                .ToList();

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(newStartDate, new TimeOnly(9, 0)));

            var localDates = (await SeriesSlotsAsync(series))
                .Select(s => DateOnly.FromDateTime(ToLocal(s.StartDateTime, trainer.TimeZoneId)))
                .Where(date => date >= newStartDate)
                .ToList();
            Assert.Equal(expectedDates, localDates);
            Assert.Equal(expectedDates[^1], (await _context.BookingSeries.SingleAsync()).MaterializedUntil);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_WithUnchangedValues_DoesNotDuplicateSlots()
        {
            // Starts tomorrow so every slot is still in the future, whatever time the test runs.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer), TodayIn(trainer.TimeZoneId).AddDays(1));
            var startsBefore = (await SeriesSlotsAsync(series)).Select(s => s.StartDateTime).ToList();

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate, series.StartTime));

            Assert.Equal(startsBefore, (await SeriesSlotsAsync(series)).Select(s => s.StartDateTime));
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_DoesNotDuplicateTodaysSessionThatAlreadyStarted()
        {
            // A series materialized yesterday already holds today's 00:00 session, which has now started.
            // The clean-up only removes slots from "now", so that one is kept - re-materializing must then
            // not create a second copy of it.
            var trainer = await SeedTrainerAsync();
            var client = await SeedClientAsync(trainer);
            var today = TodayIn(trainer.TimeZoneId);
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = today,
                StartTime = new TimeOnly(0, 0),
                MaterializedUntil = today,
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(new TimeOnly(0, 0)), Zone(trainer.TimeZoneId));
            await _context.BookedSessionsSlot.AddAsync(new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                BookingSeries = series,
                Title = series.Title,
                StartDateTime = todayStartUtc,
                EndDateTime = todayStartUtc.AddHours(1),
                Status = SessionStatus.Scheduled
            });
            await _unitOfWork.Complete();
            AuthenticateAsTrainer(trainer.Id);

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(today, new TimeOnly(0, 0)));

            var todaysSlots = (await SeriesSlotsAsync(series)).Count(s => DateOnly.FromDateTime(ToLocal(s.StartDateTime, trainer.TimeZoneId)) == today);
            Assert.Equal(1, todaysSlots);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_LeavesTheSeriesValidForTheNightlyJob()
        {
            // After a start-date change the watermark must still sit on the series' new cadence,
            // otherwise the nightly materialization's alignment guard throws for this series every night.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate.AddDays(3), new TimeOnly(9, 0)));

            var nightlyRun = await Record.ExceptionAsync(() => new BookingMaterializationService(_unitOfWork, TimeProvider.System).MaterializeBookingSeriesAsync(series));
            Assert.Null(nightlyRun);
        }

        // ---- Series details change: title, description and duration ----

        [Fact]
        public async Task UpdateBookingSeriesAsync_SavesTheNewDetailsOnTheSeries()
        {
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));

            var result = await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate, series.StartTime, "evening session", "bring bands", 45));
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            var saved = await _context.BookingSeries.SingleAsync();
            Assert.Equal("evening session", saved.Title);
            Assert.Equal("bring bands", saved.Description);
            Assert.Equal(45, saved.Duration);
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_ReMaterializedSlotsCarryTheNewTitleDescriptionAndDuration()
        {
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate, series.StartTime, "evening session", "bring bands", 45));

            var slots = await SeriesSlotsAsync(series);
            Assert.NotEmpty(slots);
            Assert.All(slots, slot =>
            {
                Assert.Equal("evening session", slot.Title);
                Assert.Equal("bring bands", slot.Description);
                Assert.Equal(TimeSpan.FromMinutes(45), slot.EndDateTime - slot.StartDateTime);
            });
        }

        [Fact]
        public async Task UpdateBookingSeriesAsync_DetailsOnlyChange_KeepsTheSchedule()
        {
            // Same start date and time: slots are re-created on exactly the same instants, none lost or duplicated.
            var trainer = await SeedTrainerAsync();
            var series = await CreateSeriesThroughEndpointAsync(trainer, await SeedClientAsync(trainer));
            var startsBefore = (await SeriesSlotsAsync(series)).Select(s => s.StartDateTime).ToList();

            await _bookingSeriesController.UpdateBookingSeriesAsync(series.Id, UpdateDto(series.StartDate, series.StartTime, "evening session"));

            Assert.Equal(startsBefore, (await SeriesSlotsAsync(series)).Select(s => s.StartDateTime));
        }

        // ---- Series reads: list and single series ----

        [Fact]
        public async Task GetTrainerBookingSeriesAsync_ReturnsOnlyTheTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var alice = await SeedClientAsync(trainer);
            await SeedSeriesAsync(trainer, alice);
            await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.GetTrainerBookingSeriesAsync();
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<List<BookingSeriesDto>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            var single = Assert.Single(response.Data!);
            Assert.Equal(alice.Id, single.ClientId);
            Assert.Equal("alice", single.ClientName);
        }

        [Fact]
        public async Task GetTrainerBookingSeriesAsync_ReturnsAnEmptyListWhenTrainerHasNoSeries()
        {
            var trainer = await SeedTrainerAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.GetTrainerBookingSeriesAsync();
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<List<BookingSeriesDto>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task GetBookingSeriesByIdAsync_ReturnsTheSeriesDetails()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer);
            var series = await SeedSeriesAsync(trainer, alice);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.GetBookingSeriesByIdAsync(series.Id);
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<BookingSeriesDto>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(series.Id, response.Data!.Id);
            Assert.Equal("alice", response.Data.ClientName);
            Assert.Equal(series.StartDate, response.Data.StartDate);
            Assert.Equal(series.StartTime, response.Data.StartTime);
            Assert.Equal(series.Recurrence, response.Data.Recurrence);
        }

        [Fact]
        public async Task GetBookingSeriesByIdAsync_ReturnsNotFoundForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.GetBookingSeriesByIdAsync(otherSeries.Id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBookingSeriesByIdAsync_ReturnsNotFoundWhenSeriesDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.GetBookingSeriesByIdAsync(999);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Defaults match the seeded / endpoint-created series, so start date tests only change the schedule
        private static BookingSeriesUpdateDto UpdateDto(DateOnly startDate, TimeOnly startTime, string title = "weekly session", string? description = "notes", int duration = 60) => new()
        {
            Title = title,
            Description = description,
            Duration = duration,
            StartDate = startDate,
            StartTime = startTime
        };

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john", string timeZoneId = SupportedTimeZones.London)
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer, TimeZoneId = timeZoneId };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();
            return trainer;
        }

        private async Task<Client> SeedClientAsync(Trainer trainer, string firstName = "alice")
        {
            var client = new Client { FirstName = firstName, Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return client;
        }

        private async Task<BookingSeries> SeedSeriesAsync(Trainer trainer, Client client)
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow);
            var series = new BookingSeries
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "weekly session",
                Duration = 60,
                StartDate = start,
                StartTime = new TimeOnly(9, 0),
                MaterializedUntil = start,
                Recurrence = SessionRecurrence.Weekly
            };
            await _context.BookingSeries.AddAsync(series);
            await _unitOfWork.Complete();
            return series;
        }

        private static TimeZoneInfo Zone(string timeZoneId) => TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        private static DateOnly TodayIn(string timeZoneId) =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone(timeZoneId)));

        private static DateTime ToLocal(DateTime utc, string timeZoneId) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone(timeZoneId));

        // Creates a series through the real endpoint so its slots are materialized exactly as in production.
        private async Task<BookingSeries> CreateSeriesThroughEndpointAsync(Trainer trainer, Client client, DateOnly? startDate = null, TimeOnly? startTime = null)
        {
            AuthenticateAsTrainer(trainer.Id);
            var dto = CreateDto(client.Id, trainer.TimeZoneId);
            dto.StartDate = startDate ?? dto.StartDate;
            dto.StartTime = startTime ?? dto.StartTime;
            await _bookingSeriesController.CreateBookingSeriesAsync(dto);
            return await _context.BookingSeries.SingleAsync(b => b.TrainerId == trainer.Id);
        }

        private async Task<List<BookedSessionSlot>> SeriesSlotsAsync(BookingSeries series) =>
            await _context.BookedSessionsSlot.Where(s => s.BookingSeriesId == series.Id).OrderBy(s => s.StartDateTime).ToListAsync();

        // Adds a slot to a series directly, for times or statuses the materialization service never produces (past dates, completed sessions)
        private async Task<BookedSessionSlot> AddSeriesSlotAsync(BookingSeries series, DateTime startUtc, SessionStatus status = SessionStatus.Scheduled)
        {
            var slot = new BookedSessionSlot
            {
                TrainerId = series.TrainerId,
                ClientId = series.ClientId,
                BookingSeriesId = series.Id,
                Title = series.Title,
                StartDateTime = startUtc,
                EndDateTime = startUtc.AddMinutes(series.Duration),
                Status = status
            };
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();
            return slot;
        }

        private static BookingSeriesCreateDto CreateDto(int clientId, string timeZoneId = SupportedTimeZones.London) => new()
        {
            ClientId = clientId,
            Title = "weekly session",
            Description = "notes",
            Duration = 60,
            StartDate = TodayIn(timeZoneId).AddDays(1), // tomorrow - every slot is in the future regardless of the time the suite runs
            StartTime = new TimeOnly(9, 0),
            Recurrence = SessionRecurrence.Weekly
        };
    }
}
