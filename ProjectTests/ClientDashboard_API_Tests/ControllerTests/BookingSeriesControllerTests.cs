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
                new BookingMaterializationService(_unitOfWork));
            TestAuthHelpers.AttachHttpContext(_bookingSeriesController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);

        private async Task<Trainer> SeedTrainerAsync(string firstName = "john")
        {
            var trainer = new Trainer { FirstName = firstName, Surname = "doe", Role = UserRole.Trainer, TimeZoneId = SupportedTimeZones.London };
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

        private static BookingSeriesCreateDto CreateDto(int clientId) => new()
        {
            ClientId = clientId,
            Title = "weekly session",
            Description = "notes",
            Duration = 60,
            StartDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.London))),
            StartTime = new TimeOnly(9, 0),
            Recurrence = SessionRecurrence.Weekly
        };

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
        public async Task UpdateBookingSeriesStartDateAsync_UpdatesTheStartDate()
        {
            var trainer = await SeedTrainerAsync();
            var series = await SeedSeriesAsync(trainer, await SeedClientAsync(trainer));
            var newStartDate = series.StartDate.AddDays(14);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.UpdateBookingSeriesStartDateAsync(series.Id, new BookingSeriesStartDateUpdateDto { StartDate = newStartDate });
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(newStartDate, (await _context.BookingSeries.SingleAsync()).StartDate);
        }

        [Fact]
        public async Task UpdateBookingSeriesStartDateAsync_ReturnsNotFoundForAnotherTrainersSeries()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSeries = await SeedSeriesAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            var originalStartDate = otherSeries.StartDate;
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookingSeriesController.UpdateBookingSeriesStartDateAsync(otherSeries.Id, new BookingSeriesStartDateUpdateDto { StartDate = originalStartDate.AddDays(14) });

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(originalStartDate, (await _context.BookingSeries.SingleAsync()).StartDate);
        }

        [Fact]
        public async Task DeleteBookingSeriesAsync_RemovesTheSeriesAndItsSlots()
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
    }
}
