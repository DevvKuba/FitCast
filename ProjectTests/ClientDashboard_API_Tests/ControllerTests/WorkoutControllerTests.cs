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

        public Task<ApiResponseDto<string>> SendTrainerAutoWorkoutCollectionNoticeAsync(Trainer trainer, int workoutCount, DateTime date)
        {
            return Task.FromResult(new ApiResponseDto<string>
            {
                Data = "",
                Message = $"Success sending message to trainer with id: {trainer.Id}",
                Success = true
            });
        }

        public Task<ApiResponseDto<string>> SendTrainerNewClientConfigurationReminderAsync(Trainer trainer, Client client, DateTime date)
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
        private readonly INotificationService _fakeNotificationService;
        private readonly IAutoPaymentCreationService _fakeAutoPaymentService;
        private readonly ClientBlockTerminationHelper _fakeClientBlockTerminator;
        private readonly UnitOfWork _unitOfWork;
        private readonly WorkoutController _workoutController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public WorkoutControllerTests()
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
        public async Task TestSuccessfullyAddingNewClientWorkoutAutoAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;
            var duration = 60;

            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Client.AddAsync(new Client { Role = UserRole.Client, FirstName = clientName, TrainerId = trainer.Id, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount, duration);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(clientName, response.Data);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();
            Assert.NotNull(clientWorkout);
            Assert.Equal(workoutTitle, clientWorkout.WorkoutTitle);

            var client = await _context.Client.FirstOrDefaultAsync();
            Assert.Equal(1, client!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestAddNewAutoClientWorkoutReturnsNotFoundWhenTrainerDoesNotExistAsync()
        {
            AuthenticateAsTrainer(999);
            var result = await _workoutController.AddNewAutoClientWorkoutAsync("nonexistent", "workout", DateOnly.Parse("19/06/2025"), 10, 60);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewAutoClientWorkoutReturnsNotFoundWhenClientDoesNotBelongToCallerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            await _context.Client.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", TrainerId = owningTrainer.Id, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            // Query-scoped lookup: a client belonging to a different trainer is indistinguishable from a
            // nonexistent one, by design (see Choosing Between Query-Scoping and Load-Then-Authorize).
            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _workoutController.AddNewAutoClientWorkoutAsync("rob", "workout", DateOnly.Parse("19/06/2025"), 10, 60);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

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
                SessionDate = "19/06/2025",
                ExerciseCount = 10,
                Duration = 60
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
                SessionDate = "19/06/2025",
                ExerciseCount = 10,
                Duration = 60
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
                SessionDate = "19/06/2025",
                ExerciseCount = 10,
                Duration = 60
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
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 5,
                Duration = 45
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var updateDto = new WorkoutUpdateDto
            {
                Id = workout.Id,
                WorkoutTitle = "new title",
                SessionDate = "20/06/2024",
                ExerciseCount = 10,
                Duration = 60
            };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _workoutController.UpdateWorkoutDetailsAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedWorkout = await _context.Workouts.FindAsync(workout.Id);
            Assert.Equal("new title", savedWorkout!.WorkoutTitle);
            Assert.Equal(DateOnly.Parse("20/06/2024"), savedWorkout.SessionDate);
            Assert.Equal(10, savedWorkout.ExerciseCount);
            Assert.Equal(60, savedWorkout.Duration);
        }

        [Fact]
        public async Task TestUpdateWorkoutDetailsReturnsNotFoundAsync()
        {
            var updateDto = new WorkoutUpdateDto
            {
                Id = 999,
                WorkoutTitle = "new title",
                SessionDate = "20/06/2024",
                ExerciseCount = 10,
                Duration = 60
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
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 5,
                Duration = 45
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var updateDto = new WorkoutUpdateDto
            {
                Id = workout.Id,
                WorkoutTitle = "new title",
                SessionDate = "20/06/2024",
                ExerciseCount = 10,
                Duration = 60
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
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 10,
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
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 10,
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
    }
}
