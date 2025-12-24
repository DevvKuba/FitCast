using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Fake implementations for testing
    public class FakeNotificationService : INotificationService
    {
        public Task<ApiResponseDto<string>> SendClientReminderAsync(int trainerId, int clientId)
        {
            return Task.FromResult(new ApiResponseDto<string> { 
                Data = "",
                Message = $"Success sending message to client with id: {clientId}",
                Success = true });
        }

        Task<ApiResponseDto<string>> INotificationService.SendTrainerReminderAsync(int trainerId, int clientId)
        {
            return Task.FromResult(new ApiResponseDto<string> { 
                Data = "", Message = $"Success sending message to trainer with id: {trainerId}",
                Success = true });
        }
    }

    public class FakeAutoPaymentCreationService : IAutoPaymentCreationService
    {
        public Task CreatePendingPaymentAsync(Trainer trainer, Client client)
        {
            return Task.CompletedTask;
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
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly INotificationService _fakeNotificationService;
        private readonly IAutoPaymentCreationService _fakeAutoPaymentService;
        private readonly UnitOfWork _unitOfWork;
        private readonly WorkoutController _workoutController;

        public WorkoutControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            });
            _mapper = config.CreateMapper();
            _passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _userRepository = new UserRepository(_context);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository);
            
            _fakeNotificationService = new FakeNotificationService();
            _fakeAutoPaymentService = new FakeAutoPaymentCreationService();
            _workoutController = new WorkoutController(_unitOfWork, _fakeNotificationService, _fakeAutoPaymentService, _mapper);
        }

        [Fact]
        public async Task TestGetClientSpecificWorkoutsReturnsWorkoutsAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 1", SessionDate = DateOnly.Parse("19/06/2024") });
            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 2", SessionDate = DateOnly.Parse("20/06/2024") });
            await _unitOfWork.Complete();

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
            var client = new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

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
        public async Task TestGetTrainerWorkoutsReturnsWorkoutsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = "client", TrainerId = trainer.Id, Trainer = trainer, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { ClientId = client.Id, ClientName = "rob", WorkoutTitle = "Workout 1", SessionDate = DateOnly.Parse("19/06/2024"), Client = client });
            await _unitOfWork.Complete();

            var result = await _workoutController.GetWorkouts(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Single(response.Data!);
        }

        [Fact]
        public async Task TestGetTrainerWorkoutsReturnsNotFoundForNonExistentTrainerAsync()
        {
            var result = await _workoutController.GetWorkouts(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyGettingDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();
            var todaysDate = DateOnly.Parse(todaysDateString[0..10]);

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            await _workoutController.AddNewAutoClientWorkoutAsync("rob", "rob's workout", todaysDate, 10, 60);
            await _workoutController.AddNewAutoClientWorkoutAsync("mat", "mat's workout", todaysDate, 10, 60);

            var actionResult = await _workoutController.GetAllDailyClientWorkoutsAsync();
            var okResult = actionResult.Result as OkObjectResult;
            var dailyWorkouts = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(dailyWorkouts);
            Assert.True(dailyWorkouts.Success);
            Assert.Equal(2, dailyWorkouts.Data!.Count);
        }

        [Fact]
        public async Task TestGetAllDailyClientWorkoutsReturnsNotFoundWhenNoWorkoutsAsync()
        {
            var actionResult = await _workoutController.GetAllDailyClientWorkoutsAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task TestSuccessfullyGettingClientWorkoutAtDateAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, 10, 65);

            var actionResult = await _workoutController.GetClientWorkoutAtDateAsync("rob", workoutDate);
            var okResult = actionResult.Result as OkObjectResult;
            var workout = okResult!.Value as ApiResponseDto<Workout>;

            Assert.NotNull(workout);
            Assert.True(workout.Success);
            Assert.Equal(clientName, workout.Data!.ClientName);
            Assert.Equal(workoutTitle, workout.Data!.WorkoutTitle);
        }

        [Fact]
        public async Task TestGetClientWorkoutAtDateReturnsNotFoundAsync()
        {
            var clientName = "rob";
            var workoutDate = DateOnly.Parse("19/06/2025");

            var actionResult = await _workoutController.GetClientWorkoutAtDateAsync(clientName, workoutDate);
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Workout>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestSuccessfullyGettingClientWorkoutsFromDateAsync()
        {
            var clientName = "rob";
            var workoutDateOne = DateOnly.Parse("19/06/2025");
            var workoutDateTwo = DateOnly.Parse("18/06/2025");

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, "workout 1", workoutDateOne, 10, 50);
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, "workout 2", workoutDateTwo, 10, 50);

            var actionResult = await _workoutController.GetClientWorkoutsFromDateAsync(DateOnly.Parse("17/06/2025"));
            var okResult = actionResult.Result as OkObjectResult;
            var clientWorkouts = okResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(clientWorkouts);
            Assert.True(clientWorkouts.Success);
            Assert.Equal(2, clientWorkouts.Data!.Count);
        }

        [Fact]
        public async Task TestGetClientWorkoutsFromDateReturnsNotFoundAsync()
        {
            var actionResult = await _workoutController.GetClientWorkoutsFromDateAsync(DateOnly.Parse("20/06/2025"));
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<Workout>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task TestCorrectlyGettingLatestClientWorkoutAsync()
        {
            var clientName = "rob";
            var workoutDateOne = DateOnly.Parse("19/06/2025");
            var workoutDateTwo = DateOnly.Parse("18/06/2025");

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, "workout 1", workoutDateOne, 10, 55);
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, "workout 2", workoutDateTwo, 10, 55);

            var actionResult = await _workoutController.GetLatestClientWorkoutAsync(clientName);
            var okResult = actionResult.Result as OkObjectResult;
            var latestWorkout = okResult!.Value as ApiResponseDto<Workout>;

            Assert.NotNull(latestWorkout);
            Assert.True(latestWorkout.Success);
            Assert.Equal(workoutDateOne, latestWorkout.Data!.SessionDate);
        }

        [Fact]
        public async Task TestGetLatestClientWorkoutReturnsNotFoundAsync()
        {
            var actionResult = await _workoutController.GetLatestClientWorkoutAsync("nonexistent");
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Workout>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientWorkoutAutoAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;
            var duration = 60;

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = clientName, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

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
        public async Task TestAddNewAutoClientWorkoutReturnsNotFoundAsync()
        {
            var result = await _workoutController.AddNewAutoClientWorkoutAsync("nonexistent", "workout", DateOnly.Parse("19/06/2025"), 10, 60);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientWorkoutManualAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { Role = "client", FirstName = "rob", TrainerId = trainer.Id, Trainer = trainer, CurrentBlockSession = 0, TotalBlockSessions = 4, Workouts = [] };
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
        public async Task TestUpdateWorkoutDetailsSuccessfullyAsync()
        {
            var client = new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
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

            var result = await _workoutController.UpdateWorkoutDetails(updateDto);
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

            var result = await _workoutController.UpdateWorkoutDetails(updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyDeletingClientWorkoutByNameAndDateAsync()
        {
            var clientName = "rob";
            var workoutDate = DateOnly.Parse("19/06/2025");

            await _context.Client.AddAsync(new Client { Role = "client", FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, "workout 1", workoutDate, 10, 60);

            var result = await _workoutController.DeleteClientWorkoutAsync(clientName, workoutDate);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(clientName, response.Data);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();
            Assert.Null(clientWorkout);
        }

        [Fact]
        public async Task TestDeleteClientWorkoutByNameAndDateReturnsNotFoundAsync()
        {
            var result = await _workoutController.DeleteClientWorkoutAsync("nonexistent", DateOnly.Parse("19/06/2025"));
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyDeletingWorkoutByIdAsync()
        {
            var client = new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
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
        public async Task TestDeleteWorkoutByIdReturnsNotFoundWhenClientDoesNotExistAsync()
        {
            var workout = new Workout
            {
                ClientId = 999,
                ClientName = "nonexistent",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 10,
                Duration = 60
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var result = await _workoutController.DeleteWorkoutAsync(workout.Id);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}

