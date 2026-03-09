using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ServiceTests
{
    // Fake Hevy parser for testing (simulates external API calls)
    public class FakeSessionDataParser : ISessionDataParser
    {
        private readonly List<WorkoutDto> _workoutsToReturn;

        public FakeSessionDataParser(List<WorkoutDto> workoutsToReturn)
        {
            _workoutsToReturn = workoutsToReturn;
        }

        public Task<List<WorkoutDto>> CallApiForTrainerAsync(Trainer trainer)
        {
            // Simulate API call returning workouts
            return Task.FromResult(_workoutsToReturn);
        }
    }

    // Fake block termination helper for testing
    public class FakeClientBlockTerminationHelper : IClientBlockTerminationHelper
    {
        public int CallCount { get; private set; }
        public List<int> ProcessedClientIds { get; } = new();

        public Task CreateAdequateTrainerRemindersAndPaymentsAsync(Client client)
        {
            CallCount++;
            ProcessedClientIds.Add(client.Id);
            return Task.CompletedTask;
        }
    }

    public class SessionSyncServiceTests
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

        public SessionSyncServiceTests()
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
            _userRepository = new UserRepository(_context, _passwordHasher);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);
        }

        #region SyncSessionsAsync Tests

        [Fact]
        public async Task TestSyncSessionsAddsNewWorkoutForExistingClientAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 2,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Upper Body",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    TotalBlockSessions = 8,
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(1, syncedCount);

            var workouts = await _context.Workouts.ToListAsync();
            Assert.Single(workouts);
            Assert.Equal("alice - Upper Body", workouts[0].WorkoutTitle);
            Assert.Equal(client.Id, workouts[0].ClientId);

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.Equal(3, updatedClient!.CurrentBlockSession); // Incremented from 2 to 3
        }

        [Fact]
        public async Task TestSyncSessionsCreatesNewClientWhenNotExistsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "bob - Leg Day",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 6,
                    Duration = TimeSpan.FromMinutes(60)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(1, syncedCount);

            var clients = await _context.Client.ToListAsync();
            Assert.Single(clients);
            Assert.Equal("bob", clients[0].FirstName);
            Assert.Equal(trainer.Id, clients[0].TrainerId);
            Assert.Equal(1, clients[0].CurrentBlockSession); // New client starts at 1

            var workouts = await _context.Workout.ToListAsync();
            Assert.Single(workouts);
            Assert.Equal("bob - Leg Day", workouts[0].Title);
        }

        [Fact]
        public async Task TestSyncSessionsSkipsDuplicateWorkoutsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 2,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var sessionDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Add existing workout
            await _unitOfWork.WorkoutRepository.AddWorkoutAsync(client, "alice - Cardio", sessionDate, 5, TimeSpan.FromMinutes(30));
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Cardio",
                    SessionDate = sessionDate, // Same date as existing workout
                    ExerciseCount = 5,
                    Duration = TimeSpan.FromMinutes(30)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(0, syncedCount); // Duplicate not counted

            var workouts = await _context.Workout.ToListAsync();
            Assert.Single(workouts); // Still only one workout (duplicate not added)

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.Equal(2, updatedClient!.CurrentBlockSession); // Not incremented (duplicate skipped)
        }

        [Fact]
        public async Task TestSyncSessionsSkipsExcludedClientsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = ["testuser", "demo"]
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "testuser - Workout",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 5,
                    Duration = TimeSpan.FromMinutes(30)
                },
                new WorkoutDto
                {
                    Title = "demo - Workout",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 3,
                    Duration = TimeSpan.FromMinutes(20)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(0, syncedCount); // Both excluded

            var clients = await _context.Client.ToListAsync();
            Assert.Empty(clients); // No clients created

            var workouts = await _context.Workout.ToListAsync();
            Assert.Empty(workouts); // No workouts added
        }

        [Fact]
        public async Task TestSyncSessionsTriggersBlockTerminationWhenBlockCompleteAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 7, // One away from completion
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Final Session",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 10,
                    Duration = TimeSpan.FromMinutes(60)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(1, syncedCount);
            Assert.Equal(1, fakeTerminator.CallCount); // Block termination triggered
            Assert.Contains(client.Id, fakeTerminator.ProcessedClientIds);

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.Equal(8, updatedClient!.CurrentBlockSession); // Now equals TotalBlockSessions
        }

        [Fact]
        public async Task TestSyncSessionsHandlesMultipleClientsInOneTrainerAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 3,
                TotalBlockSessions = 8
            };
            var client2 = new Client
            {
                FirstName = "bob",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 5,
                TotalBlockSessions = 10
            };
            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Upper Body",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 8,
                    Duration = TimeSpan.FromMinutes(45)
                },
                new WorkoutDto
                {
                    Title = "bob - Leg Day",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 6,
                    Duration = TimeSpan.FromMinutes(50)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(2, syncedCount);

            var workouts = await _context.Workout.ToListAsync();
            Assert.Equal(2, workouts.Count);

            var updatedClient1 = await _unitOfWork.ClientRepository.GetClientByIdAsync(client1.Id);
            var updatedClient2 = await _unitOfWork.ClientRepository.GetClientByIdAsync(client2.Id);
            Assert.Equal(4, updatedClient1!.CurrentBlockSession);
            Assert.Equal(6, updatedClient2!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestSyncSessionsMixedExcludedAndValidClientsAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = ["testuser"]
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 2,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Upper Body",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 8,
                    Duration = TimeSpan.FromMinutes(45)
                },
                new WorkoutDto
                {
                    Title = "testuser - Excluded",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 5,
                    Duration = TimeSpan.FromMinutes(30)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(1, syncedCount); // Only alice synced

            var workouts = await _context.Workout.ToListAsync();
            Assert.Single(workouts);
            Assert.Equal("alice - Upper Body", workouts[0].Title);

            var clients = await _context.Client.ToListAsync();
            Assert.Single(clients); // Only alice exists (testuser not created)
        }

        [Fact]
        public async Task TestSyncSessionsHandlesEmptyWorkoutListAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>(); // Empty list

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(0, syncedCount);

            var workouts = await _context.Workout.ToListAsync();
            Assert.Empty(workouts);

            var clients = await _context.Client.ToListAsync();
            Assert.Empty(clients);
        }

        [Fact]
        public async Task TestSyncSessionsDoesNotTriggerBlockTerminationWhenNotCompleteAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 5,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Workout",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 7,
                    Duration = TimeSpan.FromMinutes(40)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(1, syncedCount);
            Assert.Equal(0, fakeTerminator.CallCount); // Block termination NOT triggered

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.Equal(6, updatedClient!.CurrentBlockSession); // Incremented but not complete
        }

        [Fact]
        public async Task TestSyncSessionsHandlesCaseInsensitiveExcludedNamesAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = ["testuser"] // Lowercase in excluded list
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "TestUser - Workout", // Mixed case in workout title
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 5,
                    Duration = TimeSpan.FromMinutes(30)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(0, syncedCount); // Excluded (case-insensitive match works)

            var clients = await _context.Client.ToListAsync();
            Assert.Empty(clients);

            var workouts = await _context.Workout.ToListAsync();
            Assert.Empty(workouts);
        }

        [Fact]
        public async Task TestSyncSessionsProcessesMultipleWorkoutsForSameClientAsync()
        {
            // Arrange
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                ExcludedNames = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                CurrentBlockSession = 2,
                TotalBlockSessions = 10
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var workoutsFromApi = new List<WorkoutDto>
            {
                new WorkoutDto
                {
                    Title = "alice - Upper Body",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                    ExerciseCount = 8,
                    Duration = TimeSpan.FromMinutes(45)
                },
                new WorkoutDto
                {
                    Title = "alice - Lower Body",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    ExerciseCount = 6,
                    Duration = TimeSpan.FromMinutes(50)
                },
                new WorkoutDto
                {
                    Title = "alice - Cardio",
                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExerciseCount = 4,
                    Duration = TimeSpan.FromMinutes(30)
                }
            };

            var fakeParser = new FakeSessionDataParser(workoutsFromApi);
            var fakeTerminator = new FakeClientBlockTerminationHelper();
            var sessionSyncService = new SessionSyncService(_unitOfWork, fakeParser, fakeTerminator);

            // Act
            var syncedCount = await sessionSyncService.SyncSessionsAsync(trainer);

            // Assert
            Assert.Equal(3, syncedCount);

            var workouts = await _context.Workout.OrderBy(w => w.SessionDate).ToListAsync();
            Assert.Equal(3, workouts.Count);
            Assert.Equal("alice - Upper Body", workouts[0].Title);
            Assert.Equal("alice - Lower Body", workouts[1].Title);
            Assert.Equal("alice - Cardio", workouts[2].Title);

            var updatedClient = await _unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            Assert.Equal(5, updatedClient!.CurrentBlockSession); // Incremented by 3 (2→5)
        }

        #endregion
    }
}
