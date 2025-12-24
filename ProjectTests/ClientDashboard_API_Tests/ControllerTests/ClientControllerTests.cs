using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
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
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;
        private readonly ClientDailyFeatureService _clientDailyFeatureService;
        private readonly ClientController _clientController;

        public ClientControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
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

            _clientDailyFeatureService = new ClientDailyFeatureService(_unitOfWork);
            _clientController = new ClientController(_unitOfWork, _clientDailyFeatureService);
        }

        [Fact]
        public async Task TestGetTrainerClientsReturnsClientsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Client.AddAsync(new Client { FirstName = "rob", Role = "client", TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { FirstName = "mark", Role = "client", TrainerId = trainer.Id, CurrentBlockSession = 2, TotalBlockSessions = 8, Workouts = [] });
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
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
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
        public async Task TestGetClientByIdReturnsClientNameAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.GetClientByIdAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);
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
        public async Task TestCorrectlyGettingCurrentClientBlockSessionAsync()
        {
            await _unitOfWork.ClientRepository.AddNewClientUnderTrainerAsync("Rob", 8, null, null);
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetCurrentClientBlockSessionAsync("Rob");
            var objectResult = actionResult.Result as OkObjectResult;
            var currentClientSession = objectResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(currentClientSession);
            Assert.Equal(0, currentClientSession.Data);
            Assert.True(currentClientSession.Success);
        }

        [Fact]
        public async Task TestGetCurrentClientBlockSessionReturnsNotFoundAsync()
        {
            var result = await _clientController.GetCurrentClientBlockSessionAsync("nonexistent");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnLastBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "mat", CurrentBlockSession = 8, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnLastBlockSessionAsync();
            var okResult = actionResult.Result as OkObjectResult;
            var clientSessions = okResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(clientSessions);
            Assert.Equal(2, clientSessions.Data!.Count);
            Assert.True(clientSessions.Success);
        }

        [Fact]
        public async Task TestGetClientsOnLastBlockSessionReturnsNotFoundAsync()
        {
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnLastBlockSessionAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnFirstBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var okResult = actionResult.Result as OkObjectResult;
            var clientSessions = okResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(clientSessions);
            Assert.Equal(2, clientSessions.Data!.Count);
            Assert.True(clientSessions.Success);
        }

        [Fact]
        public async Task TestGetClientsOnFirstBlockSessionReturnsNotFoundAsync()
        {
            await _context.Client.AddAsync(new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var notFoundResult = actionResult.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<List<string>>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Empty(response.Data!);
        }

        [Fact]
        public async Task TestGetClientPhoneNumberReturnsPhoneNumberAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
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
        public async Task TestGetClientPhoneNumberReturnsNotFoundAsync()
        {
            var result = await _clientController.GetClientPhoneNumberAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestChangeClientInformationSuccessfullyAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", IsActive = true, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var updatedClient = new Client
            {
                Id = client.Id,
                FirstName = "robert",
                IsActive = false,
                CurrentBlockSession = 2,
                TotalBlockSessions = 8,
                Role = "client",
                Workouts = []
            };

            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("robert", savedClient!.FirstName);
            Assert.False(savedClient.IsActive);
            Assert.Equal(2, savedClient.CurrentBlockSession);
            Assert.Equal(8, savedClient.TotalBlockSessions);
        }

        [Fact]
        public async Task TestChangeClientInformationReturnsNotFoundAsync()
        {
            var updatedClient = new Client
            {
                Id = 999,
                FirstName = "nonexistent",
                IsActive = true,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Role = "client",
                Workouts = []
            };

            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestChangeClientPhoneNumberSuccessfullyAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var updateDto = new ClientPhoneNumberUpdateDto
            {
                Id = client.Id,
                PhoneNumber = "9876543210"
            };

            var result = await _clientController.ChangeClientPhoneNumberAsync(updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("9876543210", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("9876543210", savedClient!.PhoneNumber);
        }

        [Fact]
        public async Task TestChangeClientPhoneNumberReturnsNotFoundAsync()
        {
            var updateDto = new ClientPhoneNumberUpdateDto
            {
                Id = 999,
                PhoneNumber = "9876543210"
            };

            var result = await _clientController.ChangeClientPhoneNumberAsync(updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientTotalSessionsAsync()
        {
            var client = new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientTotalSessionsAsync("rob", 8);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(8, savedClient!.TotalBlockSessions);
        }

        [Fact]
        public async Task TestChangeClientTotalSessionsReturnsNotFoundAsync()
        {
            var result = await _clientController.ChangeClientTotalSessionsAsync("nonexistent", 8);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientCurrentSessionAsync()
        {
            var client = new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientCurrentSessionAsync("rob", 3);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(3, savedClient!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestChangeClientCurrentSessionReturnsNotFoundAsync()
        {
            var result = await _clientController.ChangeClientCurrentSessionAsync("nonexistent", 3);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientNameAsync()
        {
            var client = new Client { Role = "client", FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.ChangeClientNameAsync("rob", "robert");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("robert", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("robert", savedClient!.FirstName);
        }

        [Fact]
        public async Task TestChangeClientNameReturnsNotFoundAsync()
        {
            var result = await _clientController.ChangeClientNameAsync("nonexistent", "robert");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUnAssignCurrentTrainerSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "rob", Role = "client", TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
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
        public async Task TestUnAssignCurrentTrainerReturnsNotFoundAsync()
        {
            var result = await _clientController.UnAssignCurrentTrainerAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientByParamsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _clientController.AddNewClientAsync("rob", 4, "1234567890", trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "rob");
            Assert.NotNull(savedClient);
            Assert.Equal(4, savedClient.TotalBlockSessions);
        }

        [Fact]
        public async Task TestAddingDuplicateClientByParamsReturnsNotFoundAsync()
        {
            await _context.Client.AddAsync(new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var result = await _clientController.AddNewClientAsync("rob", 8, "", 1);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal(1, _context.Client.Count());
        }

        [Fact]
        public async Task TestSuccessfullyAddingNewClientByBodyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = "trainer" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var clientDto = new ClientAddDto
            {
                FirstName = "rob",
                TotalBlockSessions = 4,
                PhoneNumber = "1234567890",
                TrainerId = trainer.Id
            };

            var result = await _clientController.AddNewClientObjectAsync(clientDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "rob");
            Assert.NotNull(savedClient);
        }

        [Fact]
        public async Task TestSuccessfullyRemovingClientByNameAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.RemoveClientAsync("rob");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);
            Assert.Empty(_context.Client);
        }

        [Fact]
        public async Task TestRemoveClientByNameReturnsNotFoundAsync()
        {
            var result = await _clientController.RemoveClientAsync("nonexistent");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestSuccessfullyRemovingClientByIdAsync()
        {
            var client = new Client { FirstName = "rob", Role = "client", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _clientController.RemoveClientByIdAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(client.Id.ToString(), response.Data);
            Assert.Empty(_context.Client);
        }

        [Fact]
        public async Task TestRemoveClientByIdReturnsNotFoundAsync()
        {
            var result = await _clientController.RemoveClientByIdAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}

