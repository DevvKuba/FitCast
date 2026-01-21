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
    // Fake implementations for testing
    public class FakeApiKeyEncrypter : IApiKeyEncryter
    {
        public string Encrypt(string plainText)
        {
            return $"encrypted_{plainText}";
        }

        public string Decrypt(string cipherText)
        {
            return cipherText.Replace("encrypted_", "");
        }
    }

    public class FakeSessionDataParser : ISessionDataParser
    {
        private bool _isValid;

        public FakeSessionDataParser(bool isValid = true)
        {
            _isValid = isValid;
        }

        public void SetValidationResult(bool isValid)
        {
            _isValid = isValid;
        }

        public Task<bool> IsApiKeyValidAsync(string apiKey)
        {
            return Task.FromResult(_isValid);
        }

        public Task<List<WorkoutAddDto>> FetchClientSessionsAsync(string apiKey)
        {
            return Task.FromResult(new List<WorkoutAddDto>());
        }

        public Task<List<WorkoutSummaryDto>> CallApiThroughPipelineAsync()
        {
            return Task.FromResult(new List<WorkoutSummaryDto>());
        }

        public Task<List<WorkoutSummaryDto>> CallApiForTrainerAsync(Trainer trainer)
        {
            return Task.FromResult(new List<WorkoutSummaryDto>());
        }

        public Task<List<WorkoutSummaryDto>> RetrieveWorkouts(HttpResponseMessage response)
        {
            return Task.FromResult(new List<WorkoutSummaryDto>());
        }

        public int CalculateDurationInMinutes(string startTime, string endTime)
        {
            if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
            {
                return 0;
            }

            var startDateTime = DateTime.Parse(startTime);
            var endDateTime = DateTime.Parse(endTime);

            int duration = (int)(endDateTime - startDateTime).TotalMinutes;

            return duration;
        }
    }

    public class FakeSessionSyncService : ISessionSyncService
    {
        private int _sessionCount;

        public FakeSessionSyncService(int sessionCount = 0)
        {
            _sessionCount = sessionCount;
        }

        public void SetSessionCount(int count)
        {
            _sessionCount = count;
        }

        public Task<int> SyncSessionsAsync(Trainer trainer)
        {
            return Task.FromResult(_sessionCount);
        }
    }

    public class TrainerControllerTests
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
        private readonly FakeApiKeyEncrypter _fakeEncrypter;
        private readonly FakeSessionDataParser _fakeSessionDataParser;
        private readonly FakeSessionSyncService _fakeSyncService;
        private readonly TrainerController _trainerController;

        public TrainerControllerTests()
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

            _fakeEncrypter = new FakeApiKeyEncrypter();
            _fakeSessionDataParser = new FakeSessionDataParser();
            _fakeSyncService = new FakeSessionSyncService();
            _trainerController = new TrainerController(_unitOfWork, _mapper, _fakeEncrypter, _fakeSessionDataParser, _fakeSyncService);
        }

        [Fact]
        public async Task TestRetrieveTrainerByIdReturnsTrainerAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.RetrieveTrainerByIdAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data!.FirstName);
        }

        [Fact]
        public async Task TestRetrieveTrainerByIdReturnsNotFoundAsync()
        {
            var result = await _trainerController.RetrieveTrainerByIdAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Email = "john@example.com", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var updateDto = new TrainerUpdateDto
            {
                FirstName = "jonathan",
                Surname = "doe jr",
                Email = "jonathan@example.com",
                PhoneNumber = "1234567890",
                BusinessName = "New Business",
                DefaultCurrency = "£",
                AverageSessionPrice = 50.00m
            };

            var result = await _trainerController.UpdateTrainerProfileAsync(trainer.Id, updateDto);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("jonathan", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("jonathan", savedTrainer!.FirstName);
            Assert.Equal("doe jr", savedTrainer.Surname);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileReturnsNotFoundAsync()
        {
            var updateDto = new TrainerUpdateDto
            {
                FirstName = "jonathan",
                Surname = "doe",
                Email = "jonathan@example.com"
            };

            var result = await _trainerController.UpdateTrainerProfileAsync(999, updateDto);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<Trainer>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _trainerController.UpdateClientAssignmentAsync(client.Id, trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("rob", response.Data);

            var savedClient = await _context.Client.FindAsync(client.Id);
            Assert.Equal(trainer.Id, savedClient!.TrainerId);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentReturnsNotFoundForNonExistentTrainerAsync()
        {
            var client = new Client { FirstName = "rob", Role = UserRole.Client, CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var result = await _trainerController.UpdateClientAssignmentAsync(client.Id, 999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateClientAssignmentReturnsNotFoundForNonExistentClientAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.UpdateClientAssignmentAsync(999, trainer.Id);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdatePhoneNumberSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", PhoneNumber = "1234567890", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.UpdatePhoneNumberAsync(trainer.Id, "9876543210");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("9876543210", savedTrainer!.PhoneNumber);
        }

        [Fact]
        public async Task TestUpdatePhoneNumberReturnsNotFoundAsync()
        {
            var result = await _trainerController.UpdatePhoneNumberAsync(999, "9876543210");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateWorkoutRetrievalApiKeySuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSessionDataParser.SetValidationResult(true);

            var result = await _trainerController.UpdateWorkoutRetrievalApiKeyAsync(trainer.Id, "test-api-key");
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("encrypted_test-api-key", savedTrainer!.WorkoutRetrievalApiKey);
        }

        [Fact]
        public async Task TestUpdateWorkoutRetrievalApiKeyReturnsNotFoundAsync()
        {
            var result = await _trainerController.UpdateWorkoutRetrievalApiKeyAsync(999, "test-api-key");
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateWorkoutRetrievalApiKeyReturnsBadRequestForInvalidKeyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSessionDataParser.SetValidationResult(false);

            var result = await _trainerController.UpdateWorkoutRetrievalApiKeyAsync(trainer.Id, "invalid-key");
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateTrainerRetrievalDetailsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoWorkoutRetrieval = false };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSessionDataParser.SetValidationResult(true);

            var result = await _trainerController.UpdateTrainerRetrievalDetailsAsync(trainer.Id, "test-api-key", true);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.Equal("encrypted_test-api-key", savedTrainer!.WorkoutRetrievalApiKey);
            Assert.True(savedTrainer.AutoWorkoutRetrieval);
        }

        [Fact]
        public async Task TestUpdateTrainerRetrievalDetailsReturnsNotFoundAsync()
        {
            var result = await _trainerController.UpdateTrainerRetrievalDetailsAsync(999, "test-api-key", true);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateTrainerRetrievalDetailsReturnsBadRequestForInvalidKeyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSessionDataParser.SetValidationResult(false);

            var result = await _trainerController.UpdateTrainerRetrievalDetailsAsync(trainer.Id, "invalid-key", true);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestUpdateTrainerPaymentSettingSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoPaymentSetting = false };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.UpdateTrainerPaymentSettingAsync(trainer.Id, true);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("john", response.Data);

            var savedTrainer = await _context.Trainer.FindAsync(trainer.Id);
            Assert.True(savedTrainer!.AutoPaymentSetting);
        }

        [Fact]
        public async Task TestUpdateTrainerPaymentSettingReturnsNotFoundAsync()
        {
            var result = await _trainerController.UpdateTrainerPaymentSettingAsync(999, true);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGatherAndUpdateHevyClientWorkoutsSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, WorkoutRetrievalApiKey = "encrypted_test-key" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSyncService.SetSessionCount(5);

            var result = await _trainerController.GatherAndUpdateHevyClientWorkoutsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(5, response.Data);
        }

        [Fact]
        public async Task TestGatherAndUpdateHevyClientWorkoutsReturnsZeroWhenNoSessionsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, WorkoutRetrievalApiKey = "encrypted_test-key" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _fakeSyncService.SetSessionCount(0);

            var result = await _trainerController.GatherAndUpdateHevyClientWorkoutsAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(0, response.Data);
        }

        [Fact]
        public async Task TestGatherAndUpdateHevyClientWorkoutsReturnsNotFoundAsync()
        {
            var result = await _trainerController.GatherAndUpdateHevyClientWorkoutsAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGatherAndUpdateHevyClientWorkoutsReturnsBadRequestWhenNoApiKeyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, WorkoutRetrievalApiKey = null };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.GatherAndUpdateHevyClientWorkoutsAsync(trainer.Id);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<int>;

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal(0, response.Data);
        }

        [Fact]
        public async Task TestGetWorkoutRetrievalApiKeySuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, WorkoutRetrievalApiKey = "encrypted_test-key" };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.GetWorkoutRetrievalApiKeyAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("test-key", response.Data);
        }

        [Fact]
        public async Task TestGetWorkoutRetrievalApiKeyReturnsNotFoundAsync()
        {
            var result = await _trainerController.GetWorkoutRetrievalApiKeyAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetWorkoutRetrievalApiKeyReturnsBadRequestWhenNoApiKeyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, WorkoutRetrievalApiKey = null };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.GetWorkoutRetrievalApiKeyAsync(trainer.Id);
            var badRequestResult = result.Result as BadRequestObjectResult;
            var response = badRequestResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetAutoRetrievalStatusSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoWorkoutRetrieval = true };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.GetAutoRetrievalStatusAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task TestGetAutoRetrievalStatusReturnsNotFoundAsync()
        {
            var result = await _trainerController.GetAutoRetrievalStatusAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task TestGetAutoPaymentSettingStatusSuccessfullyAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer, AutoPaymentSetting = true };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var result = await _trainerController.GetAutoPaymentSettingStatusAsync(trainer.Id);
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDto<bool>;

            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task TestGetAutoPaymentSettingStatusReturnsNotFoundAsync()
        {
            var result = await _trainerController.GetAutoPaymentSettingStatusAsync(999);
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult!.Value as ApiResponseDto<string>;

            Assert.NotNull(response);
            Assert.False(response.Success);
        }
    }
}
