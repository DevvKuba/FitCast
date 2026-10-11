using ClientDashboard_API.Authorization;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class BookedSessionSlotControllerTests
    {
        private readonly DataContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly BookedSessionSlotController _bookedSessionSlotController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public BookedSessionSlotControllerTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            var (authorizationService, currentUserAccessor, httpContextAccessor) =
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _bookedSessionSlotController = new BookedSessionSlotController(_unitOfWork, testUnitOfWork.mapper, currentUserAccessor, authorizationService);
            TestAuthHelpers.AttachHttpContext(_bookedSessionSlotController, _httpContextAccessor);
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

        private async Task<BookedSessionSlot> SeedSlotAsync(Trainer trainer, Client client, DateTime? startUtc = null)
        {
            var start = startUtc ?? DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var slot = new BookedSessionSlot
            {
                TrainerId = trainer.Id,
                ClientId = client.Id,
                Title = "ad-hoc session",
                StartDateTime = start,
                EndDateTime = start.AddHours(1),
                Status = SessionStatus.Scheduled
            };
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();
            return slot;
        }

        private static BookedSessionSlotDto SlotDto(int clientId, string title = "new session")
        {
            var start = DateTime.UtcNow.Date.AddDays(3).AddHours(10);
            return new BookedSessionSlotDto
            {
                ClientId = clientId,
                Title = title,
                Description = "notes",
                StartDateTime = start,
                EndDateTime = start.AddHours(1)
            };
        }

        [Fact]
        public async Task GetBookingSessionSlotsForTrainerAsync_ReturnsOnlyTheTrainersSlotsWithinTheNext90Days()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var alice = await SeedClientAsync(trainer);
            var today = DateTime.UtcNow.Date;

            var earlierToday = await SeedSlotAsync(trainer, alice, today.AddMinutes(1));
            var nextWeek = await SeedSlotAsync(trainer, alice, today.AddDays(7));
            await SeedSlotAsync(trainer, alice, today.AddDays(-1));
            await SeedSlotAsync(trainer, alice, today.AddDays(90));
            await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"), today.AddDays(2));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.GetBookingSessionSlotsForTrainerAsync();
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<List<BookedSessionSlot>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal([earlierToday.Id, nextWeek.Id], response.Data!.Select(s => s.Id));
        }

        [Fact]
        public async Task GetBookingSessionSlotsForTrainerAsync_ReturnsNotFoundWhenTrainerDoesNotExist()
        {
            AuthenticateAsTrainer(999);

            var result = await _bookedSessionSlotController.GetBookingSessionSlotsForTrainerAsync();

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateBookingSessionSlotForTrainerAsync_UpdatesTheSlot()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer);
            var slot = await SeedSlotAsync(trainer, alice);
            var dto = SlotDto(alice.Id, "moved session");
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.UpdateBookingSessionSlotForTrainerAsync(slot.Id, dto);
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            var saved = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal("moved session", saved.Title);
            Assert.Equal(dto.StartDateTime, saved.StartDateTime);
        }

        [Fact]
        public async Task UpdateBookingSessionSlotForTrainerAsync_ReturnsNotFoundForAnotherTrainersSlot()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var bob = await SeedClientAsync(otherTrainer, "bob");
            var otherSlot = await SeedSlotAsync(otherTrainer, bob);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.UpdateBookingSessionSlotForTrainerAsync(otherSlot.Id, SlotDto(bob.Id, "hijacked"));

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("ad-hoc session", (await _context.BookedSessionsSlot.SingleAsync()).Title);
        }

        [Fact]
        public async Task UpdateBookingSessionSlotForTrainerAsync_ReturnsForbiddenWhenReassigningToAnotherTrainersClient()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var alice = await SeedClientAsync(trainer);
            var bob = await SeedClientAsync(otherTrainer, "bob");
            var slot = await SeedSlotAsync(trainer, alice);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.UpdateBookingSessionSlotForTrainerAsync(slot.Id, SlotDto(bob.Id));
            var forbiddenResult = result.Result as ObjectResult;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult!.StatusCode);
            Assert.Equal(alice.Id, (await _context.BookedSessionsSlot.SingleAsync()).ClientId);
        }

        [Fact]
        public async Task UpdateBookingSessionSlotForTrainerAsync_ReturnsNotFoundWhenClientDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();
            var slot = await SeedSlotAsync(trainer, await SeedClientAsync(trainer));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.UpdateBookingSessionSlotForTrainerAsync(slot.Id, SlotDto(999));

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddBookedSessionSlotAsync_CreatesAStandaloneSlotUnderTheTrainer()
        {
            var trainer = await SeedTrainerAsync();
            var alice = await SeedClientAsync(trainer);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.AddBookedSessionSlotAsync(SlotDto(alice.Id));
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            var saved = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(trainer.Id, saved.TrainerId);
            Assert.Equal(alice.Id, saved.ClientId);
            Assert.Null(saved.BookingSeriesId);
        }

        [Fact]
        public async Task AddBookedSessionSlotAsync_ReturnsForbiddenForAnotherTrainersClient()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var bob = await SeedClientAsync(otherTrainer, "bob");
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.AddBookedSessionSlotAsync(SlotDto(bob.Id));
            var forbiddenResult = result.Result as ObjectResult;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult!.StatusCode);
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task AddBookedSessionSlotAsync_ReturnsNotFoundWhenClientDoesNotExist()
        {
            var trainer = await SeedTrainerAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.AddBookedSessionSlotAsync(SlotDto(999));

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task DeleteBookedSessionSlotAsync_RemovesTheSlot()
        {
            var trainer = await SeedTrainerAsync();
            var slot = await SeedSlotAsync(trainer, await SeedClientAsync(trainer));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.DeleteBookedSessionSlotAsync(slot.Id);
            var response = (result.Result as OkObjectResult)!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task DeleteBookedSessionSlotAsync_ReturnsNotFoundForAnotherTrainersSlot()
        {
            var trainer = await SeedTrainerAsync("john");
            var otherTrainer = await SeedTrainerAsync("jane");
            var otherSlot = await SeedSlotAsync(otherTrainer, await SeedClientAsync(otherTrainer, "bob"));
            AuthenticateAsTrainer(trainer.Id);

            var result = await _bookedSessionSlotController.DeleteBookedSessionSlotAsync(otherSlot.Id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.True(await _context.BookedSessionsSlot.AnyAsync(s => s.Id == otherSlot.Id));
        }
    }
}
