using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class WorkoutControllerTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly TwillioMessageService _messageService;
        private readonly NotificationService _notificationService;
        private readonly AutoPaymentCreationService _autoPaymentCreationService;
        private readonly UnitOfWork _unitOfWork;
        private readonly WorkoutController _workoutController;

        public WorkoutControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Workout, WorkoutDto>();
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
            });
            _mapper = config.CreateMapper();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                // guid means a db will be created for each given test
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _clientRepository = new ClientRepository(_context, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _unitOfWork = new UnitOfWork(_context, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository);
            _messageService = new TwillioMessageService();
            _notificationService = new NotificationService(_unitOfWork, _messageService);
            _workoutController = new WorkoutController(_unitOfWork, _notificationService, _autoPaymentCreationService, _mapper);
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientWorkoutAsync()
        {
            // check current session should be increased
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.NotNull(clientWorkout);
            Assert.Equal(clientName, clientWorkout.ClientName);
            Assert.Equal(workoutTitle, clientWorkout.WorkoutTitle);
            Assert.Equal(workoutDate, clientWorkout.SessionDate);
            Assert.Equal(exerciseCount, clientWorkout.ExerciseCount);
        }

        [Fact]
        public async Task TestUnsuccessfullyAddingNewClientWorkoutAsync()
        {
            // check current session should not be increased
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.Null(clientWorkout);
        }

        [Fact]
        public async Task TestSuccessfullyDeletingClientWorkoutAsync()
        {
            // check current session should be reduced
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            await _workoutController.DeleteClientWorkoutAsync(clientName, workoutDate);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.Null(clientWorkout);
        }

        [Fact]
        public async Task TestUnsuccessfullyDeletingClientWorkoutAsync()
        {
            // check current session should be not be reduced
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            await _workoutController.DeleteClientWorkoutAsync("mat", workoutDate);

            var clientWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.NotNull(clientWorkout);
        }

        [Fact]
        public async Task TestCorrectlyGettingLatestClientWorkoutAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDateOne = DateOnly.Parse("19/06/2025");
            var workoutDateTwo = DateOnly.Parse("18/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateOne, exerciseCount);
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateTwo, exerciseCount);

            var actionResult = await _workoutController.GetLatestClientWorkoutAsync(clientName);
            // since the workout obj is in the result we need to retrieve it as OkObjectResult
            // then proceed with retrieving it's value
            var okResult = actionResult.Result as OkObjectResult;
            var latestWorkout = okResult!.Value as ApiResponseDto<Workout> ?? new ApiResponseDto<Workout> { Data = null, Message = "", Success = false };

            Assert.NotNull(latestWorkout);
            Assert.Equal(workoutDateOne, latestWorkout!.Data!.SessionDate);
        }

        [Fact]
        public async Task TestIncorrectlyGettingLatestClientWorkoutAsync()
        {
            var clientName = "rob";

            var actionResult = await _workoutController.GetLatestClientWorkoutAsync(clientName);
            // since the workout obj is in the result we need to retrieve it as OkObjectResult
            // then proceed with retrieving it's value
            var okResult = actionResult.Result as OkObjectResult;

            Assert.Null(okResult);
        }

        [Fact]
        public async Task TestSuccessfullyGettingClientWorkoutsFromDateAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDateOne = DateOnly.Parse("19/06/2025");
            var workoutDateTwo = DateOnly.Parse("18/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateOne, exerciseCount);
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateTwo, exerciseCount);

            var actionResult = await _workoutController.GetClientWorkoutsFromDateAsync(DateOnly.Parse("17/06/2025"));
            var okResult = actionResult.Result as OkObjectResult;
            var clientWorkouts = okResult!.Value as ApiResponseDto<List<Workout>> ?? new ApiResponseDto<List<Workout>> { Data = null, Message = "", Success = false };

            Assert.Equal(2, clientWorkouts!.Data!.Count);
        }

        [Fact]
        public async Task TestUnsuccessfullyGettingClientWorkoutsFromDateAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDateOne = DateOnly.Parse("19/06/2025");
            var workoutDateTwo = DateOnly.Parse("18/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateOne, exerciseCount);
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDateTwo, exerciseCount);

            var actionResult = await _workoutController.GetClientWorkoutsFromDateAsync(DateOnly.Parse("20/06/2025"));
            var okResult = actionResult.Result as OkObjectResult;

            Assert.Null(okResult);
        }

        [Fact]
        public async Task TestSuccessfullyGettingClientWorkoutAtDateAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            var actionResult = await _workoutController.GetClientWorkoutAtDateAsync("rob", DateOnly.Parse("19/06/2025"));
            var okResult = actionResult.Result as ObjectResult;
            var workout = okResult!.Value as ApiResponseDto<Workout> ?? new ApiResponseDto<Workout> { Data = null, Message = "", Success = false };

            Assert.Equal(workout!.Data!.ClientName, clientName);
            Assert.Equal(workout!.Data!.WorkoutTitle, workoutTitle);
            Assert.Equal(workout!.Data!.ExerciseCount, exerciseCount);
        }

        [Fact]
        public async Task TestUnsuccessfullyGettingClientWorkoutAtDateAsync()
        {
            var clientName = "rob";
            var workoutTitle = "workout 1";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 10;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync(clientName, workoutTitle, workoutDate, exerciseCount);

            var actionResult = await _workoutController.GetClientWorkoutAtDateAsync("rob", DateOnly.Parse("20/06/2025"));
            var okResult = actionResult.Result as ObjectResult;
            var workout = okResult!.Value as ApiResponseDto<Workout> ?? new ApiResponseDto<Workout> { Data = null, Message = "", Success = false };

            Assert.Null(workout.Data);
        }

        [Fact]
        public async Task TestSuccessfullyGettingDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();

            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync("rob", "rob's workout", DateOnly.Parse(todaysDateString[0..10]), 10);
            await _workoutController.AddNewAutoClientWorkoutAsync("mat", "mat's workout", DateOnly.Parse(todaysDateString[0..10]), 10);

            var actionResult = await _workoutController.GetAllDailyClientWorkoutsAsync();
            var okResult = actionResult.Result as ObjectResult;
            var dailyWorkouts = okResult!.Value as ApiResponseDto<List<Workout>> ?? new ApiResponseDto<List<Workout>> { Data = null, Message = "", Success = false };

            var clientOne = dailyWorkouts!.Data!.Where(x => x.ClientName == "rob").FirstOrDefault();
            var clientTwo = dailyWorkouts!.Data!.Where(x => x.ClientName == "mat").FirstOrDefault();

            Assert.Equal(2, dailyWorkouts!.Data!.Count);
            Assert.Equal(2, clientOne!.CurrentBlockSession);
            Assert.Equal(2, clientTwo!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestUnsuccessfullyGettingDailyClientWorkoutsAsync()
        {
            var date = DateOnly.Parse("19/06/2025");

            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();
            await _workoutController.AddNewAutoClientWorkoutAsync("rob", "rob's workout", date, 10);
            await _workoutController.AddNewAutoClientWorkoutAsync("mat", "mat's workout", date, 10);

            var actionResult = await _workoutController.GetAllDailyClientWorkoutsAsync();
            var okResult = actionResult.Result as ObjectResult;
            var dailyWorkouts = okResult!.Value as ApiResponseDto<List<Workout>> ?? new ApiResponseDto<List<Workout>> { Data = null, Message = "", Success = false };

            var clientOne = dailyWorkouts!.Data!.Where(x => x.ClientName == "rob").FirstOrDefault();
            var clientTwo = dailyWorkouts!.Data!. Where(x => x.ClientName == "mat").FirstOrDefault();

            Assert.Equal(0, dailyWorkouts!.Data!.Count);
            Assert.Null(clientOne);
            Assert.Null(clientTwo);
        }

    }
}
