using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Fake implementation for testing
    public class FakeClientDailyFeatureService : IClientDailyFeatureService
    {
        public Task ExecuteClientDailyGatheringAsync(Client client)
        {
            // Simulate daily data gathering without actual logic
            return Task.CompletedTask;
        }
    }

    public class ClientControllerTests
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
        private readonly IClientBlockTerminationHelper _fakeClientBlockTerminator;
        private readonly IClientDailyFeatureService _fakeClientDailyFeatureService;
        private readonly UnitOfWork _unitOfWork;
        private readonly ClientController _clientController;

        public ClientControllerTests()
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

            _fakeNotificationService = new FakeNotificationService();
            _fakeAutoPaymentService = new FakeAutoPaymentCreationService();
            _fakeClientBlockTerminator = new ClientBlockTerminationHelper(_fakeNotificationService, _fakeAutoPaymentService);
            _fakeClientDailyFeatureService = new FakeClientDailyFeatureService();
            _clientController = new ClientController(_unitOfWork, _fakeClientBlockTerminator, _fakeClientDailyFeatureService);
        }


        [Fact]
        public async Task TestGetTrainerClientsReturnsClientsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            var client2 = new Client { FirstName = "bob", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 3, TotalBlockSessions = 12 };
            await _context.Client.AddRangeAsync(client1, client2);
            await _unitOfWork.Complete();

            var result = await _clientController.GetTrainerClientsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Client>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task TestGetTrainerClientsReturnsEmptyListWhenNoClientsAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _clientController.GetTrainerClientsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Client>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }


        [Fact]
        public async Task TestGetClientByIdReturnsClientSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.GetClientByIdAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alice", response.Data);
        }

        [Fact]
        public async Task TestGetClientByIdReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.GetClientByIdAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetCurrentClientBlockSessionReturnsSessionSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 5, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.GetCurrentClientBlockSessionAsync("alice");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(5, response.Data);
        }

        [Fact]
        public async Task TestGetCurrentClientBlockSessionReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.GetCurrentClientBlockSessionAsync("NonExistent");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetClientsOnLastSessionReturnsClientsSuccessfullyAsync()
        {
            var client1 = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 8, TotalBlockSessions = 8 };
            var client2 = new Client { FirstName = "bob", Role = UserRole.Client, CurrentBlockSession = 12, TotalBlockSessions = 12 };
            var client3 = new Client { FirstName = "charlie", Role = UserRole.Client, CurrentBlockSession = 5, TotalBlockSessions = 10 };
            await _context.Client.AddRangeAsync(client1, client2, client3);
            await _unitOfWork.Complete();

            var result = await _clientController.GetClientsOnLastBlockSessionAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
            Assert.Contains("alice", response.Data);
            Assert.Contains("bob", response.Data);
        }

        [Fact]
        public async Task TestGetClientsOnLastSessionReturnsNotFoundWhenNoClientsAsync()
        {
            var result = await _clientController.GetClientsOnLastBlockSessionAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetClientsOnFirstSessionReturnsClientsSuccessfullyAsync()
        {
            var client1 = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            var client2 = new Client { FirstName = "bob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 12 };
            var client3 = new Client { FirstName = "charlie", Role = UserRole.Client, CurrentBlockSession = 5, TotalBlockSessions = 10 };
            await _context.Client.AddRangeAsync(client1, client2, client3);
            await _unitOfWork.Complete();

            var result = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
            Assert.Contains("alice", response.Data);
            Assert.Contains("bob", response.Data);
        }

        [Fact]
        public async Task TestGetClientsOnFirstSessionReturnsNotFoundWhenNoClientsAsync()
        {
            var result = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestGetClientPhoneNumberReturnsPhoneNumberSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.GetClientPhoneNumberAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("1234567890", response.Data);
        }

        [Fact]
        public async Task TestGetClientPhoneNumberReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.GetClientPhoneNumberAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestChangeClientInformationUpdatesSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client 
            { 
                FirstName = "alice", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                IsActive = true,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 8,
                PhoneNumber = "1234567890"
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var updatedClient = new Client
            {
                Id = client.Id,
                Role = UserRole.Client,
                FirstName = "alice updated",
                IsActive = false,
                CurrentBlockSession = 3,
                TotalBlockSessions = 10,
                PhoneNumber = "0987654321"
            };

            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("alice updated", savedClient!.FirstName);
            Assert.False(savedClient.IsActive);
            Assert.Equal(3, savedClient.CurrentBlockSession);
            Assert.Equal(10, savedClient.TotalBlockSessions);
        }

        [Fact]
        public async Task TestChangeClientInformationTriggersBlockTerminationWhenOnLastSessionAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = trainer.Id,
                IsActive = true,
                CurrentBlockSession = 7,
                TotalBlockSessions = 8,
                PhoneNumber = "1234567890"
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var updatedClient = new Client
            {
                Id = client.Id,
                Role = UserRole.Client,
                FirstName = "alice",
                IsActive = true,
                CurrentBlockSession = 8,
                TotalBlockSessions = 8,
                PhoneNumber = "1234567890"
            };

            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(8, savedClient!.CurrentBlockSession);
            Assert.Equal(8, savedClient.TotalBlockSessions);
        }

        [Fact]
        public async Task TestChangeClientInformationReturnsNotFoundForNonExistentClientAsync()
        {
            var updatedClient = new Client
            {
                Id = 999,
                Role = UserRole.Client,
                FirstName = "NonExistent",
                IsActive = true,
                CurrentBlockSession = 1,
                TotalBlockSessions = 8
            };

            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestChangeClientPhoneNumberUpdatesSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var phoneUpdateDto = new ClientPhoneNumberUpdateDto
            {
                Id = client.Id,
                PhoneNumber = "0987654321"
            };

            var result = await _clientController.ChangeClientPhoneNumberAsync(phoneUpdateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("0987654321", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("0987654321", savedClient!.PhoneNumber);
        }

        [Fact]
        public async Task TestChangeClientPhoneNumberReturnsNotFoundForNonExistentClientAsync()
        {
            var phoneUpdateDto = new ClientPhoneNumberUpdateDto
            {
                Id = 999,
                PhoneNumber = "0987654321"
            };

            var result = await _clientController.ChangeClientPhoneNumberAsync(phoneUpdateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestChangeClientTotalSessionsUpdatesSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientTotalSessionsAsync("alice", 12);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alice", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(12, savedClient!.TotalBlockSessions);
        }

        [Fact]
        public async Task TestChangeClientTotalSessionsReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.ChangeClientTotalSessionsAsync("NonExistent", 12);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestChangeClientCurrentSessionUpdatesSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientCurrentSessionAsync("alice", 5);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alice", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(5, savedClient!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestChangeClientCurrentSessionReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.ChangeClientCurrentSessionAsync("NonExistent", 5);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestChangeClientNameUpdatesSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientNameAsync("alice", "alicia");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alicia", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("alicia", savedClient!.FirstName);
        }

        [Fact]
        public async Task TestChangeClientNameReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.ChangeClientNameAsync("NonExistent", "NewName");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestUnAssignTrainerSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.UnAssignCurrentTrainerAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Null(savedClient!.TrainerId);
        }

        [Fact]
        public async Task TestUnAssignTrainerReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.UnAssignCurrentTrainerAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewClientByParamsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _clientController.AddNewClientAsync("alice", 8, "1234567890", trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alice", response.Data);

            var savedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "alice");
            Assert.NotNull(savedClient);
            Assert.Equal(8, savedClient.TotalBlockSessions);
            Assert.Equal("1234567890", savedClient.PhoneNumber);
            Assert.Equal(trainer.Id, savedClient.TrainerId);
        }

        [Fact]
        public async Task TestAddNewClientByParamsReturnsNotFoundWhenClientExistsAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.AddNewClientAsync("alice", 8, "1234567890", trainer.Id);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestAddNewClientByBodySuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var clientDto = new ClientAddDto
            {
                FirstName = "bob",
                TotalBlockSessions = 12,
                PhoneNumber = "0987654321",
                TrainerId = trainer.Id
            };

            var result = await _clientController.AddNewClientObjectAsync(clientDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("bob", response.Data);

            var savedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "bob");
            Assert.NotNull(savedClient);
            Assert.Equal(12, savedClient.TotalBlockSessions);
            Assert.Equal("0987654321", savedClient.PhoneNumber);
        }


        [Fact]
        public async Task TestRemoveClientByNameSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.RemoveClientAsync("alice");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("alice", response.Data);

            var deletedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "alice");
            Assert.Null(deletedClient);
        }

        [Fact]
        public async Task TestRemoveClientByNameReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.RemoveClientAsync("NonExistent");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }


        [Fact]
        public async Task TestRemoveClientByIdSuccessfullyAsync()
        {
            var client = new Client { FirstName = "alice", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.RemoveClientByIdAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(client.Id.ToString(), response.Data);

            var deletedClient = await _context.Client.FindAsync(client.Id);
            Assert.Null(deletedClient);
        }

        [Fact]
        public async Task TestRemoveClientByIdReturnsNotFoundForNonExistentClientAsync()
        {
            var result = await _clientController.RemoveClientByIdAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}