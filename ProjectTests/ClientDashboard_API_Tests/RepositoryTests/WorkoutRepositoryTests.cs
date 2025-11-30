using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class WorkoutRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly UnitOfWork _unitOfWork;

        public WorkoutRepositoryTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
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

        }

        [Fact]
        public async Task TestAddingWorkoutAsync()
        {
            var client = new Client { FirstName = "Rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var workoutTitle = "test session";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 8;

            await _workoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount);
            await _unitOfWork.Complete();

            var databaseWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.True(_context.Workouts.Any(x => x.Equals(databaseWorkout)));
            Assert.Equal(databaseWorkout!.Client, client);
            Assert.Equal(databaseWorkout!.WorkoutTitle, workoutTitle);
            Assert.Equal(databaseWorkout!.SessionDate, workoutDate);
            Assert.Equal(databaseWorkout!.ExerciseCount, exerciseCount);
        }

        [Fact]
        public async Task TestRemovingWorkoutAsync()
        {
            var client = new Client { FirstName = "Rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var workoutTitle = "test session";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 8;

            await _workoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount);
            await _unitOfWork.Complete();

            var databaseWorkout = await _context.Workouts.FirstOrDefaultAsync();
            _workoutRepository.RemoveWorkout(databaseWorkout!);
            await _unitOfWork.Complete();

            Assert.False(_context.Workouts.Any());
        }

        [Fact]
        public async Task TestGettingLatestClientWorkoutAsync()
        {
            await _context.Workouts.AddAsync(new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "test session 1",
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                SessionDate = DateOnly.Parse("19/06/2024")
            });

            await _context.Workouts.AddAsync(new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "test session 2",
                CurrentBlockSession = 2,
                TotalBlockSessions = 6,
                SessionDate = DateOnly.Parse("19/06/2025")
            });

            await _unitOfWork.Complete();

            var latestClientWorkout = await _workoutRepository.GetLatestClientWorkoutAsync("rob");

            Assert.Equal("rob", latestClientWorkout!.ClientName);
            Assert.Equal("test session 2", latestClientWorkout!.WorkoutTitle);
            Assert.Equal(2, latestClientWorkout!.CurrentBlockSession);
            Assert.Equal(6, latestClientWorkout!.TotalBlockSessions);
        }

        [Fact]
        public async Task TestGettingClientWorkoutsFromDateAsync()
        {
            await _context.Workouts.AddAsync(new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024")
            });

            await _context.Workouts.AddAsync(new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 2",
                SessionDate = DateOnly.Parse("19/06/2025")
            });

            await _context.Workouts.AddAsync(new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 3",
                SessionDate = DateOnly.Parse("19/06/2025")
            });

            await _unitOfWork.Complete();

            var mockDate = "18/06/2025";

            var recentWorkouts = await _workoutRepository.GetClientWorkoutsFromDateAsync(DateOnly.Parse(mockDate));

            Assert.True(_context.Workouts.Any(x => x.WorkoutTitle == "workout 2"));
            Assert.True(_context.Workouts.Any(x => x.WorkoutTitle == "workout 3"));
        }

        [Fact]
        public async Task TestGettingExistingtClientWorkoutAtDateAsync()
        {
            var testWorkout = new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(testWorkout);

            await _unitOfWork.Complete();

            var workout = await _workoutRepository.GetClientWorkoutAtDateByNameAsync("rob", DateOnly.Parse("19/06/2024"));

            Assert.Equal(workout, testWorkout);
        }

        [Fact]
        public async Task TestGettingNonExistingtClientWorkoutAtDateAsync()
        {
            var testWorkout = new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2025")
            };
            await _context.Workouts.AddAsync(testWorkout);

            await _unitOfWork.Complete();

            var workout = await _workoutRepository.GetClientWorkoutAtDateByNameAsync("rob", DateOnly.Parse("19/06/2024"));

            Assert.NotEqual(workout, testWorkout);
        }

        [Fact]
        public async Task TestGettingExistingtClientWorkoutsAtDateAsync()
        {
            var testWorkoutOne = new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(testWorkoutOne);

            var testWorkoutTwo = new Workout
            {
                ClientName = "mat",
                WorkoutTitle = "workout 3",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(testWorkoutTwo);

            await _unitOfWork.Complete();

            var workouts = await _workoutRepository.GetClientWorkoutsAtDateAsync(DateOnly.Parse("19/06/2024"));

            Assert.True(workouts!.Any(x => x.WorkoutTitle == "workout 1"));
            Assert.True(workouts!.Any(x => x.WorkoutTitle == "workout 3"));
        }
    }
}
