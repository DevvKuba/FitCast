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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly INotificationService _fakeNotificationService;
        private readonly IAutoPaymentCreationService _fakeAutoPaymentService;
        private readonly IClientBlockTerminationHelper _fakeClientBlockTerminator;
        private readonly UnitOfWork _unitOfWork;
        private readonly ClientController _clientController;
        private readonly FakeHttpContextAccessor _httpContextAccessor;

        public ClientControllerTests()
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
                TestAuthHelpers.CreateAuthInfrastructure(new ClientOwnershipHandler());
            _httpContextAccessor = httpContextAccessor;

            _clientController = new ClientController(_unitOfWork, _fakeClientBlockTerminator, authorizationService, currentUserAccessor);
            TestAuthHelpers.AttachHttpContext(_clientController, _httpContextAccessor);
        }

        private void AuthenticateAsTrainer(int trainerId) => TestAuthHelpers.SetCurrentUser(_httpContextAccessor, "Trainer", trainerId);

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

            AuthenticateAsTrainer(trainer.Id);
            var result = await _clientController.GetTrainerClientsAsync();
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

            AuthenticateAsTrainer(trainer.Id);
            var result = await _clientController.GetTrainerClientsAsync();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<List<Client>>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }


        [Fact]
        public async Task TestGetClientByIdReturnsClientSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestGetClientByIdReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.GetClientByIdAsync(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetClientPhoneNumberReturnsPhoneNumberSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestGetClientPhoneNumberReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = owningTrainer.Id, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.GetClientPhoneNumberAsync(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
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

            AuthenticateAsTrainer(trainer.Id);
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

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestChangeClientInformationReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client
            {
                FirstName = "alice",
                Role = UserRole.Client,
                TrainerId = owningTrainer.Id,
                IsActive = true,
                CurrentBlockSession = 1,
                TotalBlockSessions = 8
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var updatedClient = new Client
            {
                Id = client.Id,
                Role = UserRole.Client,
                FirstName = "alice updated",
                IsActive = true,
                CurrentBlockSession = 1,
                TotalBlockSessions = 8
            };

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.ChangeClientInformationAsync(updatedClient);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("alice", savedClient!.FirstName);
        }


        [Fact]
        public async Task TestChangeClientPhoneNumberUpdatesSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var phoneUpdateDto = new ClientPhoneNumberUpdateDto
            {
                Id = client.Id,
                PhoneNumber = "0987654321"
            };

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestChangeClientPhoneNumberReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = owningTrainer.Id, PhoneNumber = "1234567890", CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var phoneUpdateDto = new ClientPhoneNumberUpdateDto
            {
                Id = client.Id,
                PhoneNumber = "0987654321"
            };

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.ChangeClientPhoneNumberAsync(phoneUpdateDto);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal("1234567890", savedClient!.PhoneNumber);
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

            AuthenticateAsTrainer(trainer.Id);
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
        public async Task TestUnAssignTrainerReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.UnAssignCurrentTrainerAsync(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(owningTrainer.Id, savedClient!.TrainerId);
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

            AuthenticateAsTrainer(trainer.Id);
            var result = await _clientController.AddNewClientAsync(clientDto);
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
        public async Task TestAddNewClientReturnsBadRequestWhenTrainerIdDoesNotMatchCallerAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(trainer, otherTrainer);
            await _unitOfWork.Complete();

            var clientDto = new ClientAddDto
            {
                FirstName = "bob",
                TotalBlockSessions = 12,
                PhoneNumber = "0987654321",
                TrainerId = otherTrainer.Id
            };

            AuthenticateAsTrainer(trainer.Id);
            var result = await _clientController.AddNewClientAsync(clientDto);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FirstOrDefaultAsync(c => c.FirstName == "bob");
            Assert.Null(savedClient);
        }


        [Fact]
        public async Task TestRemoveClientByIdSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = trainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(trainer.Id);
            var result = await _clientController.RemoveClientByIdAsync(client.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(client.Id.ToString(), response.Data);

            var deletedClient = await _context.Client.FindAsync(client.Id);
            Assert.True(deletedClient!.IsDeleted);
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

        [Fact]
        public async Task TestRemoveClientByIdReturnsForbiddenForNonOwningTrainerAsync()
        {
            var owningTrainer = new Trainer { FirstName = "John", Surname = "Doe", Role = UserRole.Trainer };
            var otherTrainer = new Trainer { FirstName = "Jane", Surname = "Smith", Role = UserRole.Trainer };
            await _context.Trainer.AddRangeAsync(owningTrainer, otherTrainer);
            await _unitOfWork.Complete();

            var client = new Client { FirstName = "alice", Role = UserRole.Client, TrainerId = owningTrainer.Id, CurrentBlockSession = 1, TotalBlockSessions = 8 };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            AuthenticateAsTrainer(otherTrainer.Id);
            var result = await _clientController.RemoveClientByIdAsync(client.Id);
            var forbiddenResult = result.Result as ObjectResult;
            var response = forbiddenResult!.Value as ApiResponseDto<string>;

            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            Assert.NotNull(response);
            Assert.False(response.Success);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.False(savedClient!.IsDeleted);
        }
    }
}
