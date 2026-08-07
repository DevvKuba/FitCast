using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class WorkoutRepositoryTests
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
        private readonly UnitOfWork _unitOfWork;

        public WorkoutRepositoryTests()
        {
            _mapper = TestMapperFactory.Create();
            _passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                // guid means a db will be created for each given test
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

        }

        [Fact]
        public async Task TestAddingWorkoutAsync()
        {
            var client = new Client { Role = UserRole.Client, FirstName = "Rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var workoutTitle = "test session";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 8;
            var duration = 60; // 60 minutes

            await _workoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount, duration);
            await _unitOfWork.Complete();

            var databaseWorkout = await _context.Workouts.FirstOrDefaultAsync();

            Assert.True(_context.Workouts.Any(x => x.Equals(databaseWorkout)));
            Assert.Equal(databaseWorkout!.WorkoutTitle, workoutTitle);
            Assert.Equal(databaseWorkout!.SessionDate, workoutDate);
            Assert.Equal(databaseWorkout!.ExerciseCount, exerciseCount);
        }

        [Fact]
        public async Task TestRemovingWorkoutAsync()
        {
            var client = new Client { Role = UserRole.Client, FirstName = "Rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            var workoutTitle = "test session";
            var workoutDate = DateOnly.Parse("19/06/2025");
            var exerciseCount = 8;
            var duration = 60; // 60 minutes

            await _workoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount, duration);
            await _unitOfWork.Complete();

            var databaseWorkout = await _context.Workouts.FirstOrDefaultAsync();
            _workoutRepository.RemoveWorkout(databaseWorkout!);
            await _unitOfWork.Complete();

            Assert.False(_context.Workouts.Any());
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
        public async Task TestGetSpecificClientsWorkoutsAsync()
        {
            var client1 = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            var client2 = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "mark", 
                CurrentBlockSession = 2, 
                TotalBlockSessions = 8, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client1);
            await _context.Client.AddAsync(client2);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout
            {
                ClientId = client1.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024"),
                Client = client1
            });
            await _context.Workouts.AddAsync(new Workout
            {
                ClientId = client2.Id,
                ClientName = "mark",
                WorkoutTitle = "workout 2",
                SessionDate = DateOnly.Parse("20/06/2024"),
                Client = client2
            });
            await _unitOfWork.Complete();

            var clients = await _context.Client.Include(c => c.Workouts).ToListAsync();
            var workouts = _workoutRepository.GetSpecificClientsWorkoutsAsync(clients);

            Assert.Equal(2, workouts.Count);
            Assert.Contains(workouts, w => w.WorkoutTitle == "workout 1");
            Assert.Contains(workouts, w => w.WorkoutTitle == "workout 2");
            Assert.All(workouts, w => Assert.Null(w.Client));
            Assert.Equal("workout 2", workouts[0].WorkoutTitle);
        }

        [Fact]
        public async Task TestGetWorkoutByIdAsync()
        {
            var workout = new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "test workout",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var retrievedWorkout = await _workoutRepository.GetWorkoutByIdAsync(workout.Id);

            Assert.NotNull(retrievedWorkout);
            Assert.Equal(workout.Id, retrievedWorkout.Id);
            Assert.Equal("test workout", retrievedWorkout.WorkoutTitle);
        }

        [Fact]
        public async Task TestGetWorkoutByIdReturnsNullForNonExistentIdAsync()
        {
            var workout = await _workoutRepository.GetWorkoutByIdAsync(999);

            Assert.Null(workout);
        }

        [Fact]
        public async Task TestGetWorkoutByIdWithClientAsync()
        {
            var client = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "test workout",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            var retrievedWorkout = await _workoutRepository.GetWorkoutByIdWithClientAsync(workout.Id);

            Assert.NotNull(retrievedWorkout);
            Assert.Equal(workout.Id, retrievedWorkout.Id);
            Assert.NotNull(retrievedWorkout.Client);
            Assert.Equal(client.Id, retrievedWorkout.Client.Id);
        }

        [Fact]
        public async Task TestGetWorkoutByIdWithClientReturnsNullForNonExistentIdAsync()
        {
            var workout = await _workoutRepository.GetWorkoutByIdWithClientAsync(999);

            Assert.Null(workout);
        }

        [Fact]
        public async Task TestGetClientWorkoutsAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024"),
                Client = client
            });
            await _context.Workouts.AddAsync(new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 2",
                SessionDate = DateOnly.Parse("20/06/2024"),
                Client = client
            });
            await _unitOfWork.Complete();

            var workouts = await _workoutRepository.GetClientWorkoutsAsync(client);

            Assert.Equal(2, workouts.Count);
            Assert.All(workouts, w => Assert.Null(w.Client));
            Assert.Equal("workout 2", workouts[0].WorkoutTitle);
            Assert.Equal("workout 1", workouts[1].WorkoutTitle);
        }

        [Fact]
        public async Task TestGetClientWorkoutAtDateByIdAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var testWorkout = new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024")
            };
            await _context.Workouts.AddAsync(testWorkout);
            await _unitOfWork.Complete();

            var workout = await _workoutRepository.GetClientWorkoutAtDateByIdAsync(client.Id, DateOnly.Parse("19/06/2024"));

            Assert.NotNull(workout);
            Assert.Equal(testWorkout.Id, workout.Id);
            Assert.Equal("workout 1", workout.WorkoutTitle);
        }

        [Fact]
        public async Task TestGetClientWorkoutAtDateByIdReturnsNullForDifferentDateAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout
            {
                ClientId = client.Id,
                ClientName = "rob",
                WorkoutTitle = "workout 1",
                SessionDate = DateOnly.Parse("19/06/2024")
            });
            await _unitOfWork.Complete();

            var workout = await _workoutRepository.GetClientWorkoutAtDateByIdAsync(client.Id, DateOnly.Parse("20/06/2024"));

            Assert.Null(workout);
        }

        [Fact]
        public async Task TestGetSessionCountLast7DaysAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var untilDate = DateOnly.Parse("20/06/2024");
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("10/06/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("15/06/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("18/06/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("20/06/2024") });
            await _unitOfWork.Complete();

            var count = await _workoutRepository.GetSessionCountLast7DaysAsync(client, untilDate);

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task TestGetSessionCountLast28DaysAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var untilDate = DateOnly.Parse("20/06/2024");
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("20/05/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("25/05/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("10/06/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("15/06/2024") });
            await _context.Workouts.AddAsync(new Workout {WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("20/06/2024") });
            await _unitOfWork.Complete();

            var count = await _workoutRepository.GetSessionCountLast28DaysAsync(client, untilDate);

            Assert.Equal(4, count);
        }

        [Fact]
        public async Task TestGetDaysFromLastSessionAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("15/06/2024") });
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("18/06/2024") });
            await _unitOfWork.Complete();

            var untilDate = DateOnly.Parse("25/06/2024");
            var days = await _workoutRepository.GetDaysFromLastSessionAsync(client, untilDate);

            Assert.NotNull(days);
            Assert.Equal(7, days);
        }

        [Fact]
        public async Task TestGetDaysFromLastSessionReturnsNullWhenNoWorkoutsAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var untilDate = DateOnly.Parse("25/06/2024");
            var days = await _workoutRepository.GetDaysFromLastSessionAsync(client, untilDate);

            Assert.Null(days);
        }

        [Fact]
        public async Task TestUpdateWorkoutAsync()
        {
            var workout = new Workout
            {
                ClientName = "rob",
                WorkoutTitle = "old title",
                SessionDate = DateOnly.Parse("19/06/2024"),
                ExerciseCount = 5,
                Duration = 45
            };
            await _context.Workouts.AddAsync(workout);
            await _unitOfWork.Complete();

            _workoutRepository.UpdateWorkout(workout, "new title", DateOnly.Parse("20/06/2024"), 10, 60);
            await _unitOfWork.Complete();

            Assert.Equal("new title", workout.WorkoutTitle);
            Assert.Equal(DateOnly.Parse("20/06/2024"), workout.SessionDate);
            Assert.Equal(10, workout.ExerciseCount);
            Assert.Equal(60, workout.Duration);
        }

        [Fact]
        public async Task TestCalculateClientMeanWorkoutDurationAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 1", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("15/06/2024"), Duration = 40 });
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 2", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("17/06/2024"), Duration = 50 });
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 3" , ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("19/06/2024"), Duration = 60 });
            await _context.Workouts.AddAsync(new Workout { WorkoutTitle = "workout 4", ClientName = "rob", ClientId = client.Id, SessionDate = DateOnly.Parse("25/06/2024"), Duration = 70 });
            await _unitOfWork.Complete();

            var tillDate = DateOnly.Parse("20/06/2024");
            var meanDuration = await _workoutRepository.CalculateClientMeanWorkoutDurationAsync(client, tillDate);

            Assert.Equal(50, meanDuration);
        }

        [Fact]
        public async Task TestCalculateClientMeanWorkoutDurationReturnsZeroWhenNoWorkoutsAsync()
        {
            var client = new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4, 
                Workouts = [] 
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var tillDate = DateOnly.Parse("20/06/2024");
            var meanDuration = await _workoutRepository.CalculateClientMeanWorkoutDurationAsync(client, tillDate);

            Assert.Equal(0, meanDuration);
        }
    }
}


