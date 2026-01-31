using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class ClientRepositoryTests
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

        public ClientRepositoryTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
            });
            _mapper = config.CreateMapper();
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
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);

        }

        [Fact]
        public async Task TestAddingCorrectClientAsync()
        {
            var testClient = new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 0, TotalBlockSessions = 8 };

            await _clientRepository.AddNewClientUnderTrainerAsync(clientName: "Rob", blockSessions: 8, phoneNumber: null, trainerId: null);
            await _unitOfWork.Complete();
            var databaseClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(databaseClient!.FirstName, testClient.FirstName);
            Assert.Equal(databaseClient.CurrentBlockSession, testClient.CurrentBlockSession);
            Assert.Equal(databaseClient.TotalBlockSessions, testClient.TotalBlockSessions);
            Assert.Equal(databaseClient.Workouts.Count, testClient.Workouts.Count);
        }

        [Fact]
        public async Task TestRemovingClientCorrectlyAsync()
        {
            await _clientRepository.AddNewClientUnderTrainerAsync(clientName: "Rob", blockSessions: 8, phoneNumber: null, trainerId: null);
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            _clientRepository.RemoveClient(client!);
            await _unitOfWork.Complete();

            Assert.False(_context.Client.Any());
        }

        [Fact]
        public async Task TestCheckingIfExistingClientExistsAsync()
        {
            await _clientRepository.AddNewClientUnderTrainerAsync(clientName: "Rob", blockSessions: 8, phoneNumber: null, trainerId: null);
            await _unitOfWork.Complete();
            var clientName = "rob";

            bool clientPresent = await _clientRepository.CheckIfClientExistsAsync(clientName);

            Assert.True(clientPresent);
        }

        [Fact]
        public async Task TestGettingAllClientsOnLastSessionsAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4, Workouts = [] });
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "mark", CurrentBlockSession = 8, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var clientList = await _clientRepository.GetClientsOnLastSessionAsync();

            Assert.True(_context.Client.Any(x => x.FirstName == "rob"));
            Assert.True(_context.Client.Any(x => x.FirstName == "mark"));
        }

        [Fact]
        public async Task TestGettingAllClientsOnFirstSessionsAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "mark", CurrentBlockSession = 1, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var clientList = await _clientRepository.GetClientsOnFirstSessionAsync();

            Assert.True(_context.Client.Any(x => x.FirstName == "rob"));
            Assert.True(_context.Client.Any(x => x.FirstName == "mark"));
        }

        [Fact]
        public async Task TestGettingClientsCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var clientSession = await _clientRepository.GetClientsCurrentSessionAsync("rob");
            var expectedSessions = 2;

            Assert.Equal(clientSession, expectedSessions);
        }

        [Fact]
        public async Task TestGettingClientByNameAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByNameAsync("rob");
            var databaseClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(client, databaseClient);
        }


        [Fact]
        public async Task TestUpdateAddingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 3;
            _clientRepository.UpdateAddingClientCurrentSessionAsync(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdateAddingClientCurrentSessionNewBlockAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 1;
            _clientRepository.UpdateAddingClientCurrentSessionAsync(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdateDeletingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 1;
            _clientRepository.UpdateDeletingClientCurrentSession(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdatingClientTotalBlockSessionsAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newBlockSessions = 8;
            _clientRepository.UpdateClientTotalBlockSession(client!, newBlockSessions);
            await _unitOfWork.Complete();

            Assert.Equal(newBlockSessions, client!.TotalBlockSessions);
        }

        [Fact]
        public async Task TestUpdatingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            _clientRepository.UpdateClientCurrentSession(client!, newCurrentSession);
            await _unitOfWork.Complete();

            Assert.Equal(newCurrentSession, client!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestUpdatingClientNameAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newName = "robert";
            _clientRepository.UpdateClientName(client!, newName);
            await _unitOfWork.Complete();

            Assert.Equal(newName, client!.FirstName);
        }

        [Fact]
        public async Task TestGetAllTrainerClientDataAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, IsActive = true, TrainerId = trainer.Id, Workouts = [] });
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "mark", CurrentBlockSession = 1, TotalBlockSessions = 8, IsActive = false, TrainerId = trainer.Id, Workouts = [] });
            await _unitOfWork.Complete();

            var clients = await _clientRepository.GetAllTrainerClientDataAsync(trainer.Id);

            Assert.Equal(2, clients.Count);
            Assert.Contains(clients, c => c.FirstName == "rob");
            Assert.Contains(clients, c => c.FirstName == "mark");
            Assert.True(clients[0].IsActive);
        }

        [Fact]
        public async Task TestUpdateClientDetailsUponRegisterationAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var registerDto = new RegisterDto
            {
                Email = "rob@example.com",
                FirstName = "Rob",
                Surname = "Smith",
                PhoneNumber = "123 456 789",
                Password = "password123",
                ConfirmPassword = "password123",
                Role = UserRole.Client
            };

            _clientRepository.UpdateClientDetailsUponRegisterationAsync(trainer, client!, registerDto);
            await _unitOfWork.Complete();

            Assert.Equal("Smith", client!.Surname);
            Assert.Equal("rob@example.com", client.Email);
            Assert.Equal("123456789", client.PhoneNumber);
            Assert.NotNull(client.PasswordHash);
            Assert.NotEqual("password123", client.PasswordHash);
        }

        [Fact]
        public async Task TestUpdateClientDetailsAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, IsActive = true, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            _clientRepository.UpdateClientDetailsAsync(client!, "Robert", false, 3, 6);
            await _unitOfWork.Complete();

            Assert.Equal("robert", client!.FirstName);
            Assert.False(client.IsActive);
            Assert.Equal(3, client.CurrentBlockSession);
            Assert.Equal(6, client.TotalBlockSessions);
        }

        [Fact]
        public async Task TestUpdateClientPhoneNumberAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, PhoneNumber = "123456789", Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newPhoneNumber = "987654321";
            _clientRepository.UpdateClientPhoneNumber(client!, newPhoneNumber);
            await _unitOfWork.Complete();

            Assert.Equal(newPhoneNumber, client!.PhoneNumber);
        }

        [Fact]
        public async Task TestGetClientByNameUnderTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, TrainerId = trainer.Id, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByNameUnderTrainer(trainer, "rob");

            Assert.NotNull(client);
            Assert.Equal("rob", client.FirstName);
            Assert.Equal(trainer.Id, client.TrainerId);
        }

        [Fact]
        public async Task TestGetClientByNameUnderTrainerReturnsNullForDifferentTrainerAsync()
        {
            var trainer1 = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var trainer2 = new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer };
            await _context.AddAsync(trainer1);
            await _context.AddAsync(trainer2);
            await _unitOfWork.Complete();

            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, TrainerId = trainer1.Id, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByNameUnderTrainer(trainer2, "rob");

            Assert.Null(client);
        }

        [Fact]
        public async Task TestGetClientByIdAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var expectedClient = await _context.Client.FirstOrDefaultAsync();
            var client = await _clientRepository.GetClientByIdAsync(expectedClient!.Id);

            Assert.NotNull(client);
            Assert.Equal(expectedClient.Id, client.Id);
            Assert.Equal("rob", client.FirstName);
        }

        [Fact]
        public async Task TestGetClientByIdWithWorkoutsAsync()
        {
            await _context.AddAsync(new Client 
            { 
                Role = UserRole.Client, 
                FirstName = "rob", 
                CurrentBlockSession = 2, 
                TotalBlockSessions = 4, 
                Workouts = [new Workout { ClientName = "rob", WorkoutTitle = "rob's workout"}] 
            });

            await _unitOfWork.Complete();

            var expectedClient = await _context.Client.FirstOrDefaultAsync();
            var client = await _clientRepository.GetClientByIdWithWorkoutsAsync(expectedClient!.Id);

            Assert.NotNull(client);
            Assert.Equal(expectedClient.Id, client.Id);
            Assert.NotNull(client.Workouts);
            Assert.Single(client.Workouts);
        }

        [Fact]
        public async Task TestGetClientByIdWithTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, TrainerId = trainer.Id, Workouts = [] });
            await _unitOfWork.Complete();

            var expectedClient = await _context.Client.FirstOrDefaultAsync();
            var client = await _clientRepository.GetClientByIdWithTrainerAsync(expectedClient!.Id);

            Assert.NotNull(client);
            Assert.Equal(expectedClient.Id, client.Id);
            Assert.NotNull(client.Trainer);
            Assert.Equal("john", client.Trainer.FirstName);
        }

        [Fact]
        public async Task TestGetClientByEmailAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", Email = "rob@example.com", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByEmailAsync("rob@example.com");

            Assert.NotNull(client);
            Assert.Equal("rob@example.com", client.Email);
            Assert.Equal("rob", client.FirstName);
        }

        [Fact]
        public async Task TestGetClientByEmailReturnsNullForNonExistentEmailAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", Email = "rob@example.com", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByEmailAsync("nonexistent@example.com");

            Assert.Null(client);
        }

        [Fact]
        public async Task TestGatherDailyClientStepsAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, DailySteps = 10000, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var steps = _clientRepository.GatherDailyClientStepsAsync(client!);

            Assert.Equal(10000, steps);
        }

        [Fact]
        public async Task TestUnassignTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, TrainerId = trainer.Id, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            _clientRepository.UnassignTrainerAsync(client!);
            await _unitOfWork.Complete();

            Assert.Null(client!.TrainerId);
        }

        [Fact]
        public async Task TestAddNewClientUserAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.AddAsync(trainer);
            await _unitOfWork.Complete();

            var clientData = new Client
            {
                FirstName = "Rob",
                Surname = "smith",
                PhoneNumber = "123456789",
                Role = UserRole.Client,
            };

            var newClient = await _clientRepository.AddNewClientUserAsync(clientData, trainer.Id);
            await _unitOfWork.Complete();

            var databaseClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "rob");

            Assert.NotNull(databaseClient);
            Assert.Equal("rob", databaseClient.FirstName);
            Assert.Equal("smith", databaseClient.Surname);
            Assert.Equal("123456789", databaseClient.PhoneNumber);
            Assert.Equal(trainer.Id, databaseClient.TrainerId);
            Assert.True(databaseClient.IsActive);
            Assert.Equal(UserRole.Client, databaseClient.Role);
        }

        [Fact]
        public async Task TestUpdateDeletingClientCurrentSessionAtZeroAsync()
        {
            await _context.AddAsync(new Client { Role = UserRole.Client, FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var expectedSession = 4;
            _clientRepository.UpdateDeletingClientCurrentSession(client!);

            Assert.Equal(expectedSession, client!.CurrentBlockSession);
        }
    }
}
