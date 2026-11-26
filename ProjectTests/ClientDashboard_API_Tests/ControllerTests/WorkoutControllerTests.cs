using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Fake implementations for testing
    public class FakeNotificationService : INotificationService
    {
        public Task<ApiResponseDto<string>> SendClientBlockReminderAsync(int trainerId, int clientId)
        {
            return Task.FromResult(new ApiResponseDto<string> {
                Data = "",
                Message = $"Success sending message to client with id: {clientId}",
                Success = true
            });
        }

        public Task<ApiResponseDto<string>> SendTrainerDailySessionSyncNoticeAsync(Trainer trainer, int syncedCount, DateTime date)
        {
            return Task.FromResult(new ApiResponseDto<string>
            {
                Data = "",
                Message = $"Success sending message to trainer with id: {trainer.Id}",
                Success = true
            });
        }


        public Task<ApiResponseDto<string>> SendTrainerPendingPaymentAlertAsync(int trainerId, int clientId)
        {
            return Task.FromResult(new ApiResponseDto<string>
            {
                Data = "",
                Message = $"Success sending message to trainer with id: {trainerId}",
                Success = true
            });
        }

        Task<ApiResponseDto<string>> INotificationService.SendTrainerBlockReminderAsync(int trainerId, int clientId)
        {
            return Task.FromResult(new ApiResponseDto<string> {
                Data = "", Message = $"Success sending message to trainer with id: {trainerId}",
                Success = true
            });
        }

        public Task<ApiResponseDto<string>> SendQuickAddTrainerReminderAsync(Trainer trainer, Client client, DateTime date)
        {
            return Task.FromResult(new ApiResponseDto<string>
            {
                Data = "",
                Message = $"Success sending quick add reminder to trainer with id: {trainer.Id}",
                Success = true
            });
        }
    }

    public class FakeAutoPaymentCreationService : IAutoPaymentCreationService
    {
        public Task<ApiResponseDto<string>> CreatePendingPaymentAsync(Trainer trainer, Client client)
        {
            return Task.FromResult(new ApiResponseDto<string>
            {
                Data = "",
                Message = $"Success creating pending payment for client with id: {client.Id}",
                Success = true
            });
        }
    }

    public class WorkoutControllerTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly INotificationService _fakeNotificationService;
        private readonly IAutoPaymentCreationService _fakeAutoPaymentService;
        private readonly ClientBlockTerminationHelper _fakeClientBlockTerminator;
        private readonly UnitOfWork _unitOfWork;
        private readonly WorkoutController _workoutController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public WorkoutControllerTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _mapper = testUnitOfWork.mapper;
            _context = testUnitOfWork.context;
            _unitOfWork = testUnitOfWork.unitOfWork;

            _fakeNotificationService = new FakeNotificationService();
            _fakeAutoPaymentService = new FakeAutoPaymentCreationService();
            _fakeClientBlockTerminator = new ClientBlockTerminationHelper(_fakeNotificationService, _fakeAutoPaymentService);

            var (authorizationService, currentUserAccessor, httpContextAccessor) =
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler(), new WorkoutOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _workoutController = new WorkoutController(_unitOfWork, _fakeNotificationService, _fakeClientBlockTerminator, _mapper, authorizationService, currentUserAccessor);
            TestAuthHelpers.AttachHttpContext(_workoutController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);
        private void AuthenticateAsClient(int clientId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Client", clientId);

        [Fact]
        public async Task TestGetClientSpecificWorkoutsReturnsWorkoutsAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 1", SessionDate = DateOnly.Parse("19/06/2024") });
            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 2", SessionDate = DateOnly.Parse("20/06/2024") });
            await _unitOfWork.Complete();

            AuthenticateAsClient(client.Id);
            var result = await _workoutController.GetClientSpecificWorkouts(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task TestGetClientSpecificWorkoutsReturnsEmptyListWhenNoWorkoutsAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsClient(client.Id);
            var result = await _workoutController.GetClientSpecificWorkouts(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task TestGetClientSpecificWorkoutsReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _workoutController.GetClientSpecificWorkouts(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetClientSpecificWorkoutsReturnsForbiddenForDifferentClientAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var otherClient = new Client { FirstName = "sam", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddRangeAsync(client, otherClient);
            await _unitOfWork.Complete();

            AuthenticateAsClient(otherClient.Id);
            var result = await _workoutController.GetClientSpecificWorkouts(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetTrainerWorkoutsReturnsWorkoutsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = UserRole.Client, TrainerId = trainer.Id, Trainer = trainer, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 1", SessionDate = DateOnly.Parse("19/06/2024"), Client = client });
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.GetWorkoutsAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Single(response.Data!);
        }

        [Fact]
        public async Task TestGetTrainerWorkoutsReturnsNotFoundForNonExistentTrainerAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _workoutController.GetWorkoutsAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientWorkoutManualAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = trainer.Id, Trainer = trainer, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutDto = new WorkoutAddDto
            {
                WorkoutTitle = "workout 1",
                ClientName = "rob",
                ClientId = client.Id,
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.AddNewManualClientWorkoutAsync(workoutDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(1, savedClient!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestAddNewManualClientWorkoutReturnsNotFoundAsync()
        {
            var workoutDto = new WorkoutAddDto
            {
                WorkoutTitle = "workout 1",
                ClientName = "nonexistent",
                ClientId = 999,
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            var result = await _workoutController.AddNewManualClientWorkoutAsync(workoutDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewManualClientWorkoutReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = owningTrainer.Id, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutDto = new WorkoutAddDto
            {
                WorkoutTitle = "workout 1",
                ClientName = "rob",
                ClientId = client.Id,
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _workoutController.AddNewManualClientWorkoutAsync(workoutDto);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(0, savedClient!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestUpdateWorkoutDetailsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "old title",
                SessionDate = RecentLocalDate(2),
                Duration = 45
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var updateDto = new WorkoutUpdateDto
            {
                Id = workout.Id,
                WorkoutTitle = "new title",
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.UpdateWorkoutDetailsAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedWorkout = await _context.Workouts.FindAsync(workout.Id);
            Assert.Equal("new title", savedWorkout!.WorkoutTitle);
            Assert.Equal(RecentLocalDate(1), savedWorkout.SessionDate);
            Assert.Equal(60, savedWorkout.Duration);
        }

        [Fact]
        public async Task TestUpdateWorkoutDetailsReturnsNotFoundAsync()
        {
            var updateDto = new WorkoutUpdateDto
            {
                Id = 999,
                WorkoutTitle = "new title",
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            var result = await _workoutController.UpdateWorkoutDetailsAsync(updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateWorkoutDetailsReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "old title",
                SessionDate = RecentLocalDate(2),
                Duration = 45
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var updateDto = new WorkoutUpdateDto
            {
                Id = workout.Id,
                WorkoutTitle = "new title",
                SessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy"),
                Duration = 60,
                StartDateTime = StartUtcOn(RecentLocalDate(1), 9)
            };

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _workoutController.UpdateWorkoutDetailsAsync(updateDto);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedWorkout = await _context.Workouts.FindAsync(workout.Id);
            Assert.Equal("old title", savedWorkout!.WorkoutTitle);
        }

        [Fact]
        public async Task TestSuccessfullyDeletingWorkoutByIdAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = RecentLocalDate(1),
                Duration = 60
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.DeleteWorkoutAsync(workout.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("workout 1", response.Data);

            var deletedWorkout = await _context.Workouts.FindAsync(workout.Id);
            Assert.Null(deletedWorkout);
        }

        [Fact]
        public async Task TestDeleteWorkoutByIdReturnsNotFoundForNonExistentWorkoutAsync()
        {
            var result = await _workoutController.DeleteWorkoutAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestDeleteWorkoutByIdReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = RecentLocalDate(1),
                Duration = 60
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _workoutController.DeleteWorkoutAsync(workout.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var survivingWorkout = await _context.Workouts.FindAsync(workout.Id);
            Assert.NotNull(survivingWorkout);
        }

        [Fact]
        public async Task TestDeleteSyncedWorkoutCancelsItsSessionSlotAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = trainer.Id, CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var start = StartUtcOn(RecentLocalDate(1), 9);
            var slot = new BookedSessionSlot { TrainerId = trainer.Id, ClientId = client.Id, Title = "session", StartDateTime = start, EndDateTime = start.AddHours(1), Status = SessionStatus.Synced };
            await _context.BookedSessionsSlot.AddAsync(slot);
            await _unitOfWork.Complete();

            var workout = new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "session", SessionDate = RecentLocalDate(1), Duration = 60, BookedSessionSlotId = slot.Id };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.DeleteWorkoutAsync(workout.Id);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.False(await _context.Workouts.AnyAsync());
            Assert.Equal(SessionStatus.Cancelled, (await _context.BookedSessionsSlot.SingleAsync()).Status);
            Assert.Equal(1, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task TestManualAddWithStartTimeCreatesSyncedSlotAndLinkedWorkoutAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var start = StartUtcOn(RecentLocalDate(1), 9);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, start.ToString("dd/MM/yyyy"), 45, start));

            Assert.IsType<OkObjectResult>(result.Result);
            var slot = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(SessionStatus.Synced, slot.Status);
            Assert.Equal(start, slot.StartDateTime);
            Assert.Equal(start.AddMinutes(45), slot.EndDateTime);
            Assert.Equal("workout", slot.Title);

            var workout = await _context.Workouts.SingleAsync();
            Assert.Equal(slot.Id, workout.BookedSessionSlotId);
            Assert.Equal(start, workout.StartDateTime);
            Assert.Equal(start.AddMinutes(45), workout.EndDateTime);
            Assert.Equal(RecentLocalDate(1), workout.SessionDate);
            Assert.Equal(1, (await _context.Client.SingleAsync()).CurrentBlockSession);
        }

        [Fact]
        public async Task TestManualAddTakesTheSessionDateFromTheStartInTheTrainersZoneAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            trainer.TimeZoneId = SupportedTimeZones.Auckland;
            await _unitOfWork.Complete();
            AuthenticateAsTrainer(trainer.Id);

            // Auckland is always ahead of UTC, so 23:30 UTC is already the next day there
            var utcDay = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
            var start = utcDay.ToDateTime(new TimeOnly(23, 30), DateTimeKind.Utc);
            await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, utcDay.ToString("dd/MM/yyyy"), 60, start));

            Assert.Equal(utcDay.AddDays(1), (await _context.Workouts.SingleAsync()).SessionDate);
        }

        [Fact]
        public async Task TestManualAddReturnsBadRequestForAFutureStartAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var start = DateTime.UtcNow.AddHours(2);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, start.ToString("dd/MM/yyyy"), 60, start));

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.False(await _context.Workouts.AnyAsync());
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task TestManualAddAllowsTwoWorkoutsForAClientOnTheSameDayAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            AuthenticateAsTrainer(trainer.Id);

            var sessionDate = RecentLocalDate(1).ToString("dd/MM/yyyy");
            await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, sessionDate, 60, StartUtcOn(RecentLocalDate(1), 9)));
            var result = await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, sessionDate, 60, StartUtcOn(RecentLocalDate(1), 17)));

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, await _context.Workouts.CountAsync());
            Assert.Equal(2, await _context.BookedSessionsSlot.CountAsync());
        }

        [Fact]
        public async Task TestQuickAddCreatesASyncedSlotEndingOnTheQuarterHourWithTheDefaultLengthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.QuickAddClientWorkoutAsync(client);

            Assert.IsType<OkObjectResult>(result.Result);
            var slot = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(SessionStatus.Synced, slot.Status);
            Assert.Equal(QuickAddSessionHelper.SessionTitle, slot.Title);
            Assert.Equal(0, slot.EndDateTime.Minute % 15);
            Assert.True(slot.EndDateTime <= DateTime.UtcNow);
            Assert.Equal(TimeSpan.FromMinutes(60), slot.EndDateTime - slot.StartDateTime);

            var workout = await _context.Workouts.SingleAsync();
            Assert.Equal(slot.Id, workout.BookedSessionSlotId);
            Assert.Equal(60, workout.Duration);
        }

        [Fact]
        public async Task TestQuickAddUsesTheTrainersRecentAverageSessionLengthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "recent", today.AddDays(-3), 40);
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "recent", today.AddDays(-5), 50);
            await _unitOfWork.Complete();
            AuthenticateAsTrainer(trainer.Id);

            await _workoutController.QuickAddClientWorkoutAsync(client);

            var slot = await _context.BookedSessionsSlot.SingleAsync();
            Assert.Equal(TimeSpan.FromMinutes(45), slot.EndDateTime - slot.StartDateTime);
        }

        [Fact]
        public async Task TestQuickAddAllowsASecondQuickAddOnTheSameDayAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            AuthenticateAsTrainer(trainer.Id);

            await _workoutController.QuickAddClientWorkoutAsync(client);
            var result = await _workoutController.QuickAddClientWorkoutAsync(client);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, await _context.Workouts.CountAsync());
        }

        [Fact]
        public async Task TestUpdateWorkoutWithStartTimeMovesTheLinkedSlotAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var (workout, slot) = await SeedSyncedWorkoutAsync(trainer, client, StartUtcOn(RecentLocalDate(2), 9));
            var newStart = StartUtcOn(RecentLocalDate(1), 14);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.UpdateWorkoutDetailsAsync(UpdateDto(workout.Id, "renamed", RecentLocalDate(1).ToString("dd/MM/yyyy"), 30, newStart));

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(newStart, workout.StartDateTime);
            Assert.Equal(RecentLocalDate(1), workout.SessionDate);
            Assert.Equal("renamed", slot.Title);
            Assert.Equal(newStart, slot.StartDateTime);
            Assert.Equal(newStart.AddMinutes(30), slot.EndDateTime);
        }

        [Fact]
        public async Task TestUpdateWorkoutLoggedBeforeSessionTimesGainsTheGivenTimesAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var workout = new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "old", SessionDate = RecentLocalDate(2), Duration = 60 };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();
            var start = StartUtcOn(RecentLocalDate(1), 9);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.UpdateWorkoutDetailsAsync(UpdateDto(workout.Id, "old", RecentLocalDate(1).ToString("dd/MM/yyyy"), 45, start));

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(start, workout.StartDateTime);
            Assert.Equal(start.AddMinutes(45), workout.EndDateTime);
            Assert.Equal(RecentLocalDate(1), workout.SessionDate);
        }

        [Fact]
        public async Task TestManualAddReturnsBadRequestForAClosedMonthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.AddNewManualClientWorkoutAsync(ManualAddDto(client, ClosedMonthDate().ToString("dd/MM/yyyy"), 60, StartUtcOn(ClosedMonthDate(), 9)));

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(WorkoutEditWindowHelper.LockedWorkoutMessage, (badRequest.Value as ApiResponseDto<string>)!.Message);
            Assert.False(await _context.Workouts.AnyAsync());
            Assert.False(await _context.BookedSessionsSlot.AnyAsync());
        }

        [Fact]
        public async Task TestUpdateWorkoutReturnsBadRequestForAWorkoutInAClosedMonthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var workout = new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "old", SessionDate = ClosedMonthDate(), Duration = 60 };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.UpdateWorkoutDetailsAsync(UpdateDto(workout.Id, "moved out", RecentLocalDate(1).ToString("dd/MM/yyyy"), 60, StartUtcOn(RecentLocalDate(1), 9)));

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("old", workout.WorkoutTitle);
            Assert.Equal(ClosedMonthDate(), workout.SessionDate);
        }

        [Fact]
        public async Task TestUpdateWorkoutReturnsBadRequestWhenMovedIntoAClosedMonthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var originalStart = StartUtcOn(RecentLocalDate(1), 9);
            var (workout, slot) = await SeedSyncedWorkoutAsync(trainer, client, originalStart);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.UpdateWorkoutDetailsAsync(UpdateDto(workout.Id, "session", ClosedMonthDate().ToString("dd/MM/yyyy"), 60, StartUtcOn(ClosedMonthDate(), 9)));

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(originalStart, workout.StartDateTime);
            Assert.Equal(originalStart, slot.StartDateTime);
        }

        [Fact]
        public async Task TestDeleteWorkoutReturnsBadRequestForAClosedMonthAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var workout = new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "old", SessionDate = ClosedMonthDate(), Duration = 60 };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.DeleteWorkoutAsync(workout.Id);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(await _context.Workouts.FindAsync(workout.Id));
        }

        [Fact]
        public async Task TestUpdateWorkoutReturnsBadRequestForAFutureStartAsync()
        {
            var (trainer, client) = await SeedTrainerAndClientAsync();
            var originalStart = StartUtcOn(RecentLocalDate(1), 9);
            var (workout, slot) = await SeedSyncedWorkoutAsync(trainer, client, originalStart);
            var futureStart = DateTime.UtcNow.AddDays(2);
            AuthenticateAsTrainer(trainer.Id);

            var result = await _workoutController.UpdateWorkoutDetailsAsync(UpdateDto(workout.Id, "session", futureStart.ToString("dd/MM/yyyy"), 60, futureStart));

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(originalStart, slot.StartDateTime);
        }

        private async Task<(Trainer Trainer, Client Client)> SeedTrainerAndClientAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, TimeZoneId = SupportedTimeZones.London };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = trainer.Id, Trainer = trainer, CurrentBlockSession = 0, TotalBlockSessions = 8, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();
            return (trainer, client);
        }

        private async Task<(Workout Workout, BookedSessionSlot Slot)> SeedSyncedWorkoutAsync(Trainer trainer, Client client, DateTime startUtc)
        {
            var slot = await _unitOfWork.BookedSessionSlotRepository.AddSyncedSlotAsync(trainer.Id, client.Id, "session", startUtc, startUtc.AddMinutes(60));
            await _unitOfWork.WorkoutRepository.AddSyncedWorkoutAsync(client, slot, DateOnly.FromDateTime(startUtc));
            await _unitOfWork.Complete();
            return (await _context.Workouts.SingleAsync(), slot);
        }

        private static WorkoutAddDto ManualAddDto(Client client, string sessionDate, int duration, DateTime startUtc) => new()
        {
            WorkoutTitle = "workout",
            ClientName = client.FirstName,
            ClientId = client.Id,
            SessionDate = sessionDate,
            Duration = duration,
            StartDateTime = startUtc
        };

        private static WorkoutUpdateDto UpdateDto(int workoutId, string title, string sessionDate, int duration, DateTime startUtc) => new()
        {
            Id = workoutId,
            WorkoutTitle = title,
            SessionDate = sessionDate,
            Duration = duration,
            StartDateTime = startUtc
        };

        // Any of the last 7 days is inside the edit window on whatever date the tests run, since a new month keeps the previous one open for 7 days
        private static DateOnly RecentLocalDate(int daysAgo)
        {
            return TrainerLocalDateHelper.GetToday(SupportedTimeZones.London, DateTime.UtcNow).AddDays(-daysAgo);
        }

        private static DateOnly ClosedMonthDate()
        {
            return RecentLocalDate(0).AddMonths(-3);
        }

        private static DateTime StartUtcOn(DateOnly londonDate, int londonHour)
        {
            var london = TimeZoneInfo.FindSystemTimeZoneById(SupportedTimeZones.London);
            return TimeZoneInfo.ConvertTimeToUtc(londonDate.ToDateTime(new TimeOnly(londonHour, 0)), london);
        }
    }
}
